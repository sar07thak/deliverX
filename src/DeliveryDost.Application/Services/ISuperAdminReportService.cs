using DeliveryDost.Application.DTOs.Reports;

namespace DeliveryDost.Application.Services;

/// <summary>
/// Service for Super Admin Dashboard Reports
/// </summary>
public interface ISuperAdminReportService
{
    /// <summary>
    /// Get End Consumer Report
    /// End Consumers do not require Aadhaar verification
    /// </summary>
    Task<ReportResponse<EndConsumerReportItem>> GetEndConsumerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Business Consumer Report
    /// Includes PAN, Aadhaar verification status, subscription details
    /// </summary>
    Task<ReportResponse<BusinessConsumerReportItem>> GetBusinessConsumerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Delivery Partners Report
    /// Includes service area, delivery rates, KYC status
    /// </summary>
    Task<ReportResponse<DeliveryPartnerReportItem>> GetDeliveryPartnerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DPCM Report
    /// Includes counts of users in their area, commission details
    /// </summary>
    Task<ReportResponse<DPCMReportItem>> GetDPCMReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export End Consumer Report to Excel
    /// </summary>
    Task<byte[]> ExportEndConsumerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export Business Consumer Report to Excel
    /// </summary>
    Task<byte[]> ExportBusinessConsumerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export Delivery Partner Report to Excel
    /// </summary>
    Task<byte[]> ExportDeliveryPartnerReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export DPCM Report to Excel
    /// </summary>
    Task<byte[]> ExportDPCMReportAsync(
        ReportRequest request,
        CancellationToken cancellationToken = default);
}
