namespace DeliverX.Application.DTOs.Pricing;

public class CommissionBreakdown
{
    public decimal TotalAmount { get; set; }
    public decimal DPEarning { get; set; }
    public decimal DPCMCommission { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal DPEarningPercentage { get; set; }
    public decimal DPCMCommissionPercentage { get; set; }
    public decimal PlatformFeePercentage { get; set; }
    public decimal GSTPercentage { get; set; }
}
