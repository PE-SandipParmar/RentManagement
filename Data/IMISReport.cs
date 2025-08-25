using RentManagement.Models;
using RentManagement.Models.RentPaymentSystem.Models;

namespace RentManagement.Data
{
    public interface IMISReportRepository
    {
        Task<List<LeaseReports>> GetLeaseReportAsync(ReportRequestDto request);
        Task<List<VendorReports>> GetVendorReportAsync(ReportRequestDto request);
        Task<FinancialSummaryDto> GetFinancialSummaryAsync(ReportRequestDto request);
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<MonthlyRentTrend>> GetRentTrendsAsync(int months = 6);
        Task<bool> SaveReportLogAsync(string reportType, string fileName, string status);

        //Task<ReportResponseDto<T>> GenerateReportAsync<T>(ReportRequestDto request);
        //Task<byte[]> ExportToPdfAsync<T>(List<T> data, string reportType);
        //Task<byte[]> ExportToExcelAsync<T>(List<T> data, string reportType);
        //Task<string> ExportToCsvAsync<T>(List<T> data, string reportType);
        //Task<DashboardStatsDto> GetDashboardDataAsync();

    }
}
