using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;

namespace RentManagement.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // GET: Dashboard/Index
        [HttpGet]
        public async Task<IActionResult> Index(int? year)
        {
            try
            {
                int financialYear = year ?? DateTime.Now.Year;

                // If current month is Jan-Mar, use current year as financial year
                // Otherwise use next year
                if (!year.HasValue)
                {
                    var currentMonth = DateTime.Now.Month;
                    financialYear = currentMonth <= 3 ? DateTime.Now.Year : DateTime.Now.Year + 1;
                }

                ViewBag.FinancialYear = financialYear;
                ViewBag.CurrentUser = User.Identity?.Name ?? "Admin";

                var dashboardData = await _dashboardService.GetDashboardDataAsync(financialYear);
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "An error occurred while loading the dashboard.";
                return View(new DashboardViewModel());
            }
        }

        // API Endpoints for AJAX calls

        // GET: api/Dashboard/Statistics
        [HttpGet]
        [Route("api/Dashboard/Statistics")]
        public async Task<IActionResult> GetStatistics(int? year)
        {
            try
            {
                int financialYear = year ?? DateTime.Now.Year;
                var statistics = await _dashboardService.GetStatisticsAsync(financialYear);
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard statistics");
                return Json(new { success = false, message = "Error fetching statistics" });
            }
        }

        // GET: api/Dashboard/ChartData
        [HttpGet]
        [Route("api/Dashboard/ChartData")]
        public async Task<IActionResult> GetChartData(string chartType, int? year)
        {
            try
            {
                int financialYear = year ?? DateTime.Now.Year;
                var chartData = await _dashboardService.GetChartDataAsync(chartType, financialYear);
                return Json(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching chart data for {chartType}");
                return Json(new { success = false, message = "Error fetching chart data" });
            }
        }

        // POST: api/Dashboard/ExportReport
        [HttpPost]
        [Route("api/Dashboard/ExportReport")]
        public async Task<IActionResult> ExportReport(int? year)
        {
            try
            {
                int financialYear = year ?? DateTime.Now.Year;
                var dashboardData = await _dashboardService.GetDashboardDataAsync(financialYear);

                // Generate Excel report
                var excelBytes = GenerateExcelReport(dashboardData);

                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Dashboard_Report_{financialYear}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard report");
                return Json(new { success = false, message = "Error exporting report" });
            }
        }

        private byte[] GenerateExcelReport(DashboardViewModel data)
        {
            // Implementation for Excel generation
            // You can use libraries like EPPlus or ClosedXML
            // This is a placeholder - implement based on your requirements
            return new byte[0];
        }
    }
}
