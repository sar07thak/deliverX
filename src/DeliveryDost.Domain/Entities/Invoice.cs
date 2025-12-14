namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Invoice for Business Consumer deliveries
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // INV-YYYYMM-XXXX
    public Guid BusinessConsumerId { get; set; }

    // Invoice Period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Amounts
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAmount { get; set; }

    // Tax Details
    public decimal CGSTRate { get; set; } = 9; // 9%
    public decimal SGSTRate { get; set; } = 9; // 9%
    public decimal IGSTRate { get; set; } = 18; // 18%
    public bool IsInterState { get; set; } = false;

    // Billing Details
    public string? BillingName { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingGSTIN { get; set; }
    public string? BillingPAN { get; set; }

    // Status
    public string Status { get; set; } = "DRAFT"; // DRAFT, GENERATED, SENT, PAID, OVERDUE, CANCELLED
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaymentId { get; set; }

    // File
    public string? PdfUrl { get; set; }
    public string? PdfStoragePath { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GeneratedAt { get; set; }
    public DateTime? SentAt { get; set; }

    // Navigation
    public User? BusinessConsumer { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

/// <summary>
/// Invoice line item
/// </summary>
public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? DeliveryId { get; set; }

    // Item Details
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? HSNCode { get; set; } // HSN/SAC code for GST
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Navigation
    public Invoice? Invoice { get; set; }
    public Delivery? Delivery { get; set; }
}
