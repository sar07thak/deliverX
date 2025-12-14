namespace DeliveryDost.Application.DTOs.Invoice;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid BusinessConsumerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }

    public string? PdfUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GeneratedAt { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? DeliveryId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAmount { get; set; }
}

public class GenerateInvoiceRequest
{
    public Guid BusinessConsumerId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class InvoiceListResponse
{
    public List<InvoiceSummaryDto> Invoices { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalPaid { get; set; }
}

public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class MarkInvoicePaidRequest
{
    public Guid InvoiceId { get; set; }
    public Guid PaymentId { get; set; }
}
