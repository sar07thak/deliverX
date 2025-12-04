using System.Collections.Generic;

namespace DeliverX.Application.DTOs.Pricing;

public class UpdatePlatformFeesRequest
{
    public decimal? PlatformFeePercentage { get; set; }
    public decimal? GSTPercentage { get; set; }
    public decimal? PeakHourSurcharge { get; set; }
    public List<string>? PeakHours { get; set; } // e.g., ["08:00-10:00", "18:00-21:00"]
}
