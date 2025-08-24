using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;

namespace RentManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardApiController> _logger;

        public DashboardApiController(IDashboardService dashboardService, ILogger<DashboardApiController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // GET: api/DashboardApi/GetDashboardData
        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData(int? financialYear)
        {
            try
            {
                int year = financialYear ?? GetCurrentFinancialYear();
                var data = await _dashboardService.GetDashboardDataAsync(year);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardData API");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/DashboardApi/GetStatistics
        [HttpGet("GetStatistics")]
        public async Task<IActionResult> GetStatistics(int? financialYear)
        {
            try
            {
                int year = financialYear ?? GetCurrentFinancialYear();
                var statistics = await _dashboardService.GetStatisticsAsync(year);
                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStatistics API");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/DashboardApi/GetMonthlyTrend
        [HttpGet("GetMonthlyTrend")]
        public async Task<IActionResult> GetMonthlyTrend(int? financialYear, int months = 6)
        {
            try
            {
                int year = financialYear ?? GetCurrentFinancialYear();
                var data = await _dashboardService.GetChartDataAsync("monthlytrend", year);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMonthlyTrend API");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/DashboardApi/GetPaymentStatus
        [HttpGet("GetPaymentStatus")]
        public async Task<IActionResult> GetPaymentStatus(int? financialYear)
        {
            try
            {
                int year = financialYear ?? GetCurrentFinancialYear();
                var data = await _dashboardService.GetChartDataAsync("paymentstatus", year);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentStatus API");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/DashboardApi/GetDepartmentDistribution
        [HttpGet("GetDepartmentDistribution")]
        public async Task<IActionResult> GetDepartmentDistribution(int? financialYear)
        {
            try
            {
                int year = financialYear ?? GetCurrentFinancialYear();
                var data = await _dashboardService.GetChartDataAsync("department", year);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDepartmentDistribution API");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // GET: api/DashboardApi/GetPaymentSummary
        [HttpGet("GetPaymentSummary")]
        public async Task<IActionResult> GetPaymentSummary(int? financialYear)
        {
            try
            {
                int year = financialYear ?? GetCurrentFinancialYear();
                var data = await _dashboardService.GetChartDataAsync("paymentsummary", year);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentSummary API");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        private int GetCurrentFinancialYear()
        {
            var currentMonth = DateTime.Now.Month;
            return currentMonth <= 3 ? DateTime.Now.Year : DateTime.Now.Year + 1;
        }
    }
}
