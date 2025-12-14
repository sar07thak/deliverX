using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.Invoice;
using DeliveryDost.Application.Services;
using DeliveryDost.Domain.Entities;
using DeliveryDost.Infrastructure.Data;

namespace DeliveryDost.Infrastructure.Services;

/// <summary>
/// Invoice service implementation for Business Consumer billing
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    private const decimal CGST_RATE = 9m;
    private const decimal SGST_RATE = 9m;
    private const decimal IGST_RATE = 18m;
    private const string HSN_CODE = "996812"; // HSN code for courier services

    public InvoiceService(
        ApplicationDbContext context,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<InvoiceDto>> GenerateInvoiceAsync(GenerateInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get BC profile
            var bcProfile = await _context.BusinessConsumerProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == request.BusinessConsumerId, cancellationToken);

            if (bcProfile == null)
                return Result<InvoiceDto>.Failure("Business Consumer not found");

            // Get completed deliveries in the period
            var deliveries = await _context.Deliveries
                .Where(d => d.RequesterId == request.BusinessConsumerId)
                .Where(d => d.Status == "DELIVERED")
                .Where(d => d.CreatedAt >= request.PeriodStart && d.CreatedAt <= request.PeriodEnd)
                .ToListAsync(cancellationToken);

            if (!deliveries.Any())
                return Result<InvoiceDto>.Failure("No completed deliveries found for this period");

            // Check if invoice already exists for this period
            var existingInvoice = await _context.Invoices
                .AnyAsync(i => i.BusinessConsumerId == request.BusinessConsumerId &&
                              i.PeriodStart == request.PeriodStart.Date &&
                              i.PeriodEnd == request.PeriodEnd.Date &&
                              i.Status != "CANCELLED", cancellationToken);

            if (existingInvoice)
                return Result<InvoiceDto>.Failure("Invoice already exists for this period");

            // Determine if inter-state (for now, default to intra-state)
            var isInterState = false; // In production, parse address to determine state

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = GenerateInvoiceNumber(),
                BusinessConsumerId = request.BusinessConsumerId,
                PeriodStart = request.PeriodStart.Date,
                PeriodEnd = request.PeriodEnd.Date,
                IsInterState = isInterState,
                CGSTRate = CGST_RATE,
                SGSTRate = SGST_RATE,
                IGSTRate = IGST_RATE,
                BillingName = bcProfile.BusinessName,
                BillingAddress = bcProfile.BusinessAddress,
                BillingGSTIN = bcProfile.GSTIN,
                BillingPAN = bcProfile.PAN,
                Status = "GENERATED",
                DueDate = DateTime.UtcNow.AddDays(30), // 30 days payment terms
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                GeneratedAt = DateTime.UtcNow
            };

            var lineNumber = 1;
            decimal subTotal = 0;

            foreach (var delivery in deliveries)
            {
                var unitPrice = delivery.FinalPrice ?? delivery.EstimatedPrice ?? 0;
                var taxableAmount = unitPrice;
                decimal cgst = 0, sgst = 0, igst = 0;

                if (isInterState)
                {
                    igst = Math.Round(taxableAmount * (IGST_RATE / 100), 2);
                }
                else
                {
                    cgst = Math.Round(taxableAmount * (CGST_RATE / 100), 2);
                    sgst = Math.Round(taxableAmount * (SGST_RATE / 100), 2);
                }

                var totalAmount = taxableAmount + cgst + sgst + igst;

                var item = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    DeliveryId = delivery.Id,
                    LineNumber = lineNumber++,
                    Description = $"Delivery Service - {delivery.PickupAddress} to {delivery.DropAddress}",
                    HSNCode = HSN_CODE,
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    DiscountPercent = 0,
                    DiscountAmount = 0,
                    TaxableAmount = taxableAmount,
                    CGSTAmount = cgst,
                    SGSTAmount = sgst,
                    IGSTAmount = igst,
                    TotalAmount = totalAmount
                };

                invoice.Items.Add(item);
                subTotal += unitPrice;
            }

            // Calculate totals
            invoice.SubTotal = subTotal;
            invoice.DiscountAmount = 0; // Apply any BC-specific discounts here
            invoice.TaxableAmount = subTotal - invoice.DiscountAmount;

            if (isInterState)
            {
                invoice.IGSTAmount = Math.Round(invoice.TaxableAmount * (IGST_RATE / 100), 2);
                invoice.CGSTAmount = 0;
                invoice.SGSTAmount = 0;
            }
            else
            {
                invoice.CGSTAmount = Math.Round(invoice.TaxableAmount * (CGST_RATE / 100), 2);
                invoice.SGSTAmount = Math.Round(invoice.TaxableAmount * (SGST_RATE / 100), 2);
                invoice.IGSTAmount = 0;
            }

            invoice.TotalTax = invoice.CGSTAmount + invoice.SGSTAmount + invoice.IGSTAmount;
            invoice.TotalAmount = invoice.TaxableAmount + invoice.TotalTax;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generated invoice {Number} for BC {BCId}: Total={Total}",
                invoice.InvoiceNumber, request.BusinessConsumerId, invoice.TotalAmount);

            return Result<InvoiceDto>.Success(MapToDto(invoice, bcProfile.BusinessName ?? ""));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for BC {BCId}", request.BusinessConsumerId);
            return Result<InvoiceDto>.Failure("Failed to generate invoice");
        }
    }

    public async Task<Result<InvoiceDto>> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.BusinessConsumer)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

            if (invoice == null)
                return Result<InvoiceDto>.Failure("Invoice not found");

            var bcProfile = await _context.BusinessConsumerProfiles
                .FirstOrDefaultAsync(p => p.UserId == invoice.BusinessConsumerId, cancellationToken);

            return Result<InvoiceDto>.Success(MapToDto(invoice, bcProfile?.BusinessName ?? ""));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
            return Result<InvoiceDto>.Failure("Failed to get invoice");
        }
    }

    public async Task<Result<InvoiceDto>> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Include(i => i.BusinessConsumer)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

            if (invoice == null)
                return Result<InvoiceDto>.Failure("Invoice not found");

            var bcProfile = await _context.BusinessConsumerProfiles
                .FirstOrDefaultAsync(p => p.UserId == invoice.BusinessConsumerId, cancellationToken);

            return Result<InvoiceDto>.Success(MapToDto(invoice, bcProfile?.BusinessName ?? ""));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceNumber}", invoiceNumber);
            return Result<InvoiceDto>.Failure("Failed to get invoice");
        }
    }

    public async Task<Result<InvoiceListResponse>> GetInvoicesByBusinessConsumerAsync(
        Guid businessConsumerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Invoices
                .Where(i => i.BusinessConsumerId == businessConsumerId)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var invoices = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(i => i.Items)
                .ToListAsync(cancellationToken);

            var totalPending = await _context.Invoices
                .Where(i => i.BusinessConsumerId == businessConsumerId &&
                           (i.Status == "GENERATED" || i.Status == "SENT" || i.Status == "OVERDUE"))
                .SumAsync(i => i.TotalAmount, cancellationToken);

            var totalPaid = await _context.Invoices
                .Where(i => i.BusinessConsumerId == businessConsumerId && i.Status == "PAID")
                .SumAsync(i => i.TotalAmount, cancellationToken);

            return Result<InvoiceListResponse>.Success(new InvoiceListResponse
            {
                Invoices = invoices.Select(i => new InvoiceSummaryDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    PeriodStart = i.PeriodStart,
                    PeriodEnd = i.PeriodEnd,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status,
                    DueDate = i.DueDate,
                    CreatedAt = i.CreatedAt,
                    ItemCount = i.Items.Count
                }).ToList(),
                TotalCount = totalCount,
                TotalPending = totalPending,
                TotalPaid = totalPaid
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for BC {BCId}", businessConsumerId);
            return Result<InvoiceListResponse>.Failure("Failed to get invoices");
        }
    }

    public async Task<Result<bool>> MarkInvoicePaidAsync(MarkInvoicePaidRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

            if (invoice == null)
                return Result<bool>.Failure("Invoice not found");

            if (invoice.Status == "PAID")
                return Result<bool>.Failure("Invoice is already paid");

            if (invoice.Status == "CANCELLED")
                return Result<bool>.Failure("Cannot pay cancelled invoice");

            invoice.Status = "PAID";
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaymentId = request.PaymentId;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked invoice {Number} as paid", invoice.InvoiceNumber);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice {InvoiceId} as paid", request.InvoiceId);
            return Result<bool>.Failure("Failed to mark invoice as paid");
        }
    }

    public async Task<Result<bool>> CancelInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

            if (invoice == null)
                return Result<bool>.Failure("Invoice not found");

            if (invoice.Status == "PAID")
                return Result<bool>.Failure("Cannot cancel paid invoice");

            invoice.Status = "CANCELLED";
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled invoice {Number}", invoice.InvoiceNumber);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", invoiceId);
            return Result<bool>.Failure("Failed to cancel invoice");
        }
    }

    public async Task<Result<string>> GenerateInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        // In real implementation, use a PDF generation library (iTextSharp, QuestPDF, etc.)
        // For now, return a placeholder URL
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
            return Result<string>.Failure("Invoice not found");

        // Placeholder - in production, generate actual PDF
        var pdfUrl = $"/invoices/{invoice.InvoiceNumber}.pdf";
        invoice.PdfUrl = pdfUrl;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(pdfUrl);
    }

    public async Task<Result<bool>> SendInvoiceEmailAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        // In real implementation, use email service
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
            return Result<bool>.Failure("Invoice not found");

        invoice.Status = "SENT";
        invoice.SentAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sent invoice {Number} via email", invoice.InvoiceNumber);

        return Result<bool>.Success(true);
    }

    public async Task<Result<InvoiceListResponse>> GetPendingInvoicesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var pendingStatuses = new[] { "GENERATED", "SENT", "OVERDUE" };

            var query = _context.Invoices
                .Where(i => pendingStatuses.Contains(i.Status))
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var invoices = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(i => i.Items)
                .ToListAsync(cancellationToken);

            var totalPending = await _context.Invoices
                .Where(i => pendingStatuses.Contains(i.Status))
                .SumAsync(i => i.TotalAmount, cancellationToken);

            return Result<InvoiceListResponse>.Success(new InvoiceListResponse
            {
                Invoices = invoices.Select(i => new InvoiceSummaryDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    PeriodStart = i.PeriodStart,
                    PeriodEnd = i.PeriodEnd,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status,
                    DueDate = i.DueDate,
                    CreatedAt = i.CreatedAt,
                    ItemCount = i.Items.Count
                }).ToList(),
                TotalCount = totalCount,
                TotalPending = totalPending
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending invoices");
            return Result<InvoiceListResponse>.Failure("Failed to get pending invoices");
        }
    }

    public async Task<Result<int>> GenerateMonthlyInvoicesAsync(int year, int month, CancellationToken cancellationToken)
    {
        try
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            // Get all active BCs
            var activeBCs = await _context.BusinessConsumerProfiles
                .Where(p => p.User != null && p.User.IsActive)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            var generatedCount = 0;

            foreach (var bcId in activeBCs)
            {
                var request = new GenerateInvoiceRequest
                {
                    BusinessConsumerId = bcId,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd
                };

                var result = await GenerateInvoiceAsync(request, cancellationToken);
                if (result.IsSuccess)
                    generatedCount++;
            }

            _logger.LogInformation("Generated {Count} monthly invoices for {Year}-{Month}",
                generatedCount, year, month);

            return Result<int>.Success(generatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly invoices for {Year}-{Month}", year, month);
            return Result<int>.Failure("Failed to generate monthly invoices");
        }
    }

    #region Private Helpers

    private string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }

    private InvoiceDto MapToDto(Invoice invoice, string businessName)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            BusinessConsumerId = invoice.BusinessConsumerId,
            BusinessName = businessName,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxableAmount = invoice.TaxableAmount,
            TotalTax = invoice.TotalTax,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            PaidAt = invoice.PaidAt,
            PdfUrl = invoice.PdfUrl,
            CreatedAt = invoice.CreatedAt,
            GeneratedAt = invoice.GeneratedAt,
            Items = invoice.Items.Select(i => new InvoiceItemDto
            {
                Id = i.Id,
                LineNumber = i.LineNumber,
                Description = i.Description,
                DeliveryId = i.DeliveryId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                TaxableAmount = i.TaxableAmount,
                TotalTax = i.CGSTAmount + i.SGSTAmount + i.IGSTAmount,
                TotalAmount = i.TotalAmount
            }).ToList()
        };
    }

    #endregion
}
