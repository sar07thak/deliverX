using System;

namespace DeliveryDost.Domain.Entities;

/// <summary>
/// Tracks which DPs were notified for a delivery
/// </summary>
public class DeliveryMatchingHistory
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid DPId { get; set; }
    public int MatchingAttempt { get; set; } = 1;
    public DateTime NotifiedAt { get; set; }

    // Response: ACCEPTED, REJECTED, TIMEOUT, null (no response yet)
    public string? ResponseType { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation
    public Delivery? Delivery { get; set; }
    public User? DP { get; set; }
}
