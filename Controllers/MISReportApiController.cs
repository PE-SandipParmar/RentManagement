using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models.RentPaymentSystem.Models;
using System.Text.Json;

namespace RentManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MISReportApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<MISReportController> _logger;

        public MISReportApiController(IReportService reportService, ILogger<MISReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var stats = await _reportService.GetDashboardDataAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("employee-report")]
        public async Task<ActionResult<ReportResponseDto<EmployeeReports>>> GetEmployeeReport([FromBody] ReportRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request cannot be null" });
                }

                request.ReportType = "employee";
                var report = await _reportService.GenerateReportAsync<EmployeeReports>(request);

                if (!report.Success)
                {
                    return BadRequest(report);
                }

                _logger.LogInformation("Employee report generated with {Count} records", report.Data.Count);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating employee report");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("lease-report")]
        public async Task<ActionResult<ReportResponseDto<LeaseReports>>> GetLeaseReport([FromBody] ReportRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request cannot be null" });
                }

                request.ReportType = "lease";
                var report = await _reportService.GenerateReportAsync<LeaseReports>(request);

                if (!report.Success)
                {
                    return BadRequest(report);
                }

                _logger.LogInformation("Lease report generated with {Count} records", report.Data.Count);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lease report");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("vendor-report")]
        public async Task<ActionResult<ReportResponseDto<VendorReports>>> GetVendorReport([FromBody] ReportRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request cannot be null" });
                }

                request.ReportType = "vendor";
                var report = await _reportService.GenerateReportAsync<VendorReports>(request);

                if (!report.Success)
                {
                    return BadRequest(report);
                }

                _logger.LogInformation("Vendor report generated with {Count} records", report.Data.Count);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vendor report");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("financial-summary")]
        public async Task<ActionResult<ReportResponseDto<FinancialSummaryDto>>> GetFinancialSummary([FromBody] ReportRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request cannot be null" });
                }

                request.ReportType = "financial";
                var report = await _reportService.GenerateReportAsync<FinancialSummaryDto>(request);

                if (!report.Success)
                {
                    return BadRequest(report);
                }

                _logger.LogInformation("Financial summary report generated");
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial summary");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("comprehensive-report")]
        public async Task<ActionResult> GetComprehensiveReport([FromBody] ReportRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request cannot be null" });
                }

                // Generate all report types for comprehensive report
                var employeeReport = await _reportService.GenerateReportAsync<EmployeeReports>(
                    new ReportRequestDto
                    {
                        ReportType = "employee",
                        FromDate = request.FromDate,
                        ToDate = request.ToDate,
                        ActiveOnly = request.ActiveOnly
                    });

                var leaseReport = await _reportService.GenerateReportAsync<LeaseReports>(
                    new ReportRequestDto
                    {
                        ReportType = "lease",
                        FromDate = request.FromDate,
                        ToDate = request.ToDate,
                        ActiveOnly = request.ActiveOnly
                    });

                var vendorReport = await _reportService.GenerateReportAsync<VendorReports>(
                    new ReportRequestDto
                    {
                        ReportType = "vendor",
                        ActiveOnly = request.ActiveOnly,
                        IncludeFinancials = request.IncludeFinancials
                    });

                var financialSummary = await _reportService.GenerateReportAsync<FinancialSummaryDto>(
                    new ReportRequestDto
                    {
                        ReportType = "financial",
                        FromDate = request.FromDate,
                        ToDate = request.ToDate
                    });

                var comprehensiveReport = new
                {
                    success = true,
                    message = "Comprehensive report generated successfully",
                    generatedOn = DateTime.Now,
                    summary = new
                    {
                        totalEmployees = employeeReport.Data?.Count ?? 0,
                        totalLeases = leaseReport.Data?.Count ?? 0,
                        totalVendors = vendorReport.Data?.Count ?? 0
                    },
                    employeeData = employeeReport.Data,
                    leaseData = leaseReport.Data,
                    vendorData = vendorReport.Data,
                    financialSummary = financialSummary.Data?.FirstOrDefault()
                };

                _logger.LogInformation("Comprehensive report generated");
                return Ok(comprehensiveReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating comprehensive report");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("export-pdf")]
        public async Task<ActionResult> ExportToPdf([FromBody] ExportRequestDto request)
        {
            try
            {
                if (request == null || request.Data == null)
                {
                    return BadRequest(new { message = "Invalid export request" });
                }

                byte[] pdfBytes = null;
                string fileName = $"{request.ReportType}_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                switch (request.ReportType.ToLower())
                {
                    case "employee":
                        var employees = JsonSerializer.Deserialize<List<EmployeeReports>>(request.Data.ToString());
                        pdfBytes = await _reportService.ExportToPdfAsync(employees, request.ReportType);
                        break;
                    case "lease":
                        var leases = JsonSerializer.Deserialize<List<LeaseReports>>(request.Data.ToString());
                        pdfBytes = await _reportService.ExportToPdfAsync(leases, request.ReportType);
                        break;
                    case "vendor":
                        var vendors = JsonSerializer.Deserialize<List<VendorReports>>(request.Data.ToString());
                        pdfBytes = await _reportService.ExportToPdfAsync(vendors, request.ReportType);
                        break;
                    default:
                        return BadRequest(new { message = "Unsupported report type for PDF export" });
                }

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return StatusCode(500, new { message = "Failed to generate PDF" });
                }

                _logger.LogInformation("PDF export completed for {ReportType}", request.ReportType);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("export-excel")]
        public async Task<ActionResult> ExportToExcel([FromBody] ExportRequestDto request)
        {
            try
            {
                if (request == null || request.Data == null)
                {
                    return BadRequest(new { message = "Invalid export request" });
                }

                byte[] excelBytes = null;
                string fileName = $"{request.ReportType}_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                switch (request.ReportType.ToLower())
                {
                    case "employee":
                        var employees = JsonSerializer.Deserialize<List<EmployeeReports>>(request.Data.ToString());
                        excelBytes = await _reportService.ExportToExcelAsync(employees, request.ReportType);
                        break;
                    case "lease":
                        var leases = JsonSerializer.Deserialize<List<LeaseReports>>(request.Data.ToString());
                        excelBytes = await _reportService.ExportToExcelAsync(leases, request.ReportType);
                        break;
                    case "vendor":
                        var vendors = JsonSerializer.Deserialize<List<VendorReports>>(request.Data.ToString());
                        excelBytes = await _reportService.ExportToExcelAsync(vendors, request.ReportType);
                        break;
                    default:
                        return BadRequest(new { message = "Unsupported report type for Excel export" });
                }

                if (excelBytes == null || excelBytes.Length == 0)
                {
                    return StatusCode(500, new { message = "Failed to generate Excel file" });
                }

                _logger.LogInformation("Excel export completed for {ReportType}", request.ReportType);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpPost("export-csv")]
        public async Task<ActionResult> ExportToCsv([FromBody] ExportRequestDto request)
        {
            try
            {
                if (request == null || request.Data == null)
                {
                    return BadRequest(new { message = "Invalid export request" });
                }

                string csvContent = null;
                string fileName = $"{request.ReportType}_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                switch (request.ReportType.ToLower())
                {
                    case "employee":
                        var employees = JsonSerializer.Deserialize<List<EmployeeReports>>(request.Data.ToString());
                        csvContent = await _reportService.ExportToCsvAsync(employees, request.ReportType);
                        break;
                    case "lease":
                        var leases = JsonSerializer.Deserialize<List<LeaseReports>>(request.Data.ToString());
                        csvContent = await _reportService.ExportToCsvAsync(leases, request.ReportType);
                        break;
                    case "vendor":
                        var vendors = JsonSerializer.Deserialize<List<VendorReports>>(request.Data.ToString());
                        csvContent = await _reportService.ExportToCsvAsync(vendors, request.ReportType);
                        break;
                    default:
                        return BadRequest(new { message = "Unsupported report type for CSV export" });
                }

                if (string.IsNullOrEmpty(csvContent))
                {
                    return StatusCode(500, new { message = "Failed to generate CSV content" });
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                _logger.LogInformation("CSV export completed for {ReportType}", request.ReportType);
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        [HttpGet("report-history")]
        public async Task<ActionResult> GetReportHistory()
        {
            try
            {
                // This would typically call a repository method to get report history
                // For now, returning a placeholder response
                var history = new
                {
                    success = true,
                    data = new[]
                    {
                        new
                        {
                            id = 1,
                            reportType = "employee",
                            fileName = "employee_report_20240825.pdf",
                            generatedDate = DateTime.Now.AddHours(-2),
                            status = "Completed",
                            fileSize = "245KB"
                        },
                        new
                        {
                            id = 2,
                            reportType = "lease",
                            fileName = "lease_report_20240824.xlsx",
                            generatedDate = DateTime.Now.AddDays(-1),
                            status = "Completed",
                            fileSize = "189KB"
                        }
                    }
                };

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report history");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}
