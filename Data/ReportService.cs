using System.Data;
using System.Data.SqlClient;
using Dapper;

using Microsoft.Extensions.Configuration;
using RentManagement.Data;
using RentManagement.Models.RentPaymentSystem.Models;
using RentManagement.Models;

namespace RentPaymentSystem.Repositories
{
    namespace RentPaymentSystem.Services
    {
        public class ReportService : IReportService
        {
            private readonly IMISReportRepository _repository;

            public ReportService(IMISReportRepository repository)
            {
                _repository = repository;
            }

            public async Task<ReportResponseDto<T>> GenerateReportAsync<T>(ReportRequestDto request)
            {
                try
                {
                    List<T> data = new List<T>();
                    int totalRecords = 0;

                    switch (request.ReportType.ToLower())
                    {
                        case "employee":
                            //var employees = await _repository.GetEmployeeReportAsync(request);
                            //data = employees.Cast<T>().ToList();
                            //totalRecords = employees.Count;
                            break;

                        case "lease":
                            var leases = await _repository.GetLeaseReportAsync(request);
                            data = leases.Cast<T>().ToList();
                            totalRecords = leases.Count;
                            break;

                        case "vendor":
                            var vendors = await _repository.GetVendorReportAsync(request);
                            data = vendors.Cast<T>().ToList();
                            totalRecords = vendors.Count;
                            break;

                        case "financial":
                            var financialData = await _repository.GetFinancialSummaryAsync(request);
                            data = new List<T> { (T)(object)financialData };
                            totalRecords = 1;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported report type: {request.ReportType}");
                    }

                    // Log report generation
                    await _repository.SaveReportLogAsync(
                        request.ReportType,
                        $"{request.ReportType}_report_{DateTime.Now:yyyyMMdd_HHmmss}",
                        "Completed"
                    );

                    return new ReportResponseDto<T>
                    {
                        Success = true,
                        Message = "Report generated successfully",
                        Data = data,
                        Metadata = new ReportMetadata
                        {
                            TotalRecords = totalRecords,
                            GeneratedOn = DateTime.Now,
                            ReportType = request.ReportType,
                            DateRange = new DateRange
                            {
                                FromDate = request.FromDate,
                                ToDate = request.ToDate
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    return new ReportResponseDto<T>
                    {
                        Success = false,
                        Message = $"Error generating report: {ex.Message}",
                        Data = new List<T>()
                    };
                }
            }

            public async Task<byte[]> ExportToPdfAsync<T>(List<T> data, string reportType)
            {
                // Implementation for PDF export using a library like iTextSharp or similar
                // This is a placeholder - you would implement actual PDF generation logic
                await Task.CompletedTask;
                return new byte[0];
            }

            public async Task<byte[]> ExportToExcelAsync<T>(List<T> data, string reportType)
            {
                // Implementation for Excel export using EPPlus or similar
                // This is a placeholder - you would implement actual Excel generation logic
                await Task.CompletedTask;
                return new byte[0];
            }

            public async Task<string> ExportToCsvAsync<T>(List<T> data, string reportType)
            {
                // Simple CSV export implementation
                if (!data.Any()) return string.Empty;

                var properties = typeof(T).GetProperties();
                var csv = new System.Text.StringBuilder();

                // Add header
                csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

                // Add data rows
                foreach (var item in data)
                {
                    var values = properties.Select(p =>
                    {
                        var value = p.GetValue(item);
                        return value?.ToString()?.Replace(",", ";") ?? string.Empty;
                    });
                    csv.AppendLine(string.Join(",", values));
                }

                await Task.CompletedTask;
                return csv.ToString();
            }

            public async Task<DashboardStatsDto> GetDashboardDataAsync()
            {
                return await _repository.GetDashboardStatsAsync();
            }
        }
    }
}