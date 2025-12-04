namespace DeliverX.Application.DTOs.ServiceArea;

/// <summary>
/// Request to find delivery partners whose service area covers both pickup and drop points
/// </summary>
public class FindEligibleDPsRequest
{
    /// <summary>
    /// Pickup location latitude
    /// </summary>
    public decimal PickupLat { get; set; }

    /// <summary>
    /// Pickup location longitude
    /// </summary>
    public decimal PickupLng { get; set; }

    /// <summary>
    /// Drop location latitude
    /// </summary>
    public decimal DropLat { get; set; }

    /// <summary>
    /// Drop location longitude
    /// </summary>
    public decimal DropLng { get; set; }

    /// <summary>
    /// Maximum number of DPs to return (default 20)
    /// </summary>
    public int MaxResults { get; set; } = 20;
}
