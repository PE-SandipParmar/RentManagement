using RentManagement.Models;
using RentManagement.Models.RentPaymentSystem.Models;

namespace RentManagement.Data
{
    public interface IReportService
    {
        Task<ReportResponseDto<T>> GenerateReportAsync<T>(ReportRequestDto request);
        Task<byte[]> ExportToPdfAsync<T>(List<T> data, string reportType);
        Task<byte[]> ExportToExcelAsync<T>(List<T> data, string reportType);
        Task<string> ExportToCsvAsync<T>(List<T> data, string reportType);
        Task<DashboardStatsDto> GetDashboardDataAsync();

    }
}
