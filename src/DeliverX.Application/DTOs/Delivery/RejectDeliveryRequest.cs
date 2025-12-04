namespace DeliverX.Application.DTOs.Delivery;

public class RejectDeliveryRequest
{
    // TOO_FAR, BUSY, UNFAMILIAR_AREA, OTHER
    public string Reason { get; set; } = "OTHER";
    public string? Notes { get; set; }
}
