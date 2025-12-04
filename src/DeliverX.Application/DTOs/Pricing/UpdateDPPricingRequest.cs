namespace DeliverX.Application.DTOs.Pricing;

public class UpdateDPPricingRequest
{
    public decimal? PerKmRate { get; set; }
    public decimal? PerKgRate { get; set; }
    public decimal? MinCharge { get; set; }
    public decimal? MaxDistanceKm { get; set; }
    public bool? AcceptsPriorityDelivery { get; set; }
    public decimal? PrioritySurcharge { get; set; }
    public decimal? PeakHourSurcharge { get; set; }
}
