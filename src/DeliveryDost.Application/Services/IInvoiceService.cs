using DeliveryDost.Application.Common;
using DeliveryDost.Application.DTOs.Invoice;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for managing invoices for Business Consumers
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Generate invoice for a business consumer for a given period
    /// </summary>
    Task<Result<InvoiceDto>> GenerateInvoiceAsync(GenerateInvoiceRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    Task<Result<InvoiceDto>> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>
    /// Get invoice by number
    /// </summary>
    Task<Result<InvoiceDto>> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Get all invoices for a business consumer
    /// </summary>
    Task<Result<InvoiceListResponse>> GetInvoicesByBusinessConsumerAsync(
        Guid businessConsumerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Mark invoice as paid
    /// </summary>
    Task<Result<bool>> MarkInvoicePaidAsync(MarkInvoicePaidRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Cancel invoice
    /// </summary>
    Task<Result<bool>> CancelInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>
    /// Generate and store PDF for invoice
    /// </summary>
    Task<Result<string>> GenerateInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>
    /// Send invoice via email
    /// </summary>
    Task<Result<bool>> SendInvoiceEmailAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>
    /// Get pending invoices for all business consumers (for admin)
    /// </summary>
    Task<Result<InvoiceListResponse>> GetPendingInvoicesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Generate monthly invoices for all business consumers (scheduled task)
    /// </summary>
    Task<Result<int>> GenerateMonthlyInvoicesAsync(int year, int month, CancellationToken cancellationToken);
}
