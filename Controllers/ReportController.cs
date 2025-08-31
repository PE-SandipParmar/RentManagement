using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;

namespace RentManagement.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILeaseRepository _leaseRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IVendorRepository _vendorRepository;

        public ReportController(
            IReportRepository reportRepository,
            ILeaseRepository leaseRepository,
            IEmployeeRepository employeeRepository,
            IVendorRepository vendorRepository)
        {
            _reportRepository = reportRepository;
            _leaseRepository = leaseRepository;
            _employeeRepository = employeeRepository;
            _vendorRepository = vendorRepository;
        }

        // GET: Report
        public IActionResult Index()
        {
            return View();
        }

        // GET: Report/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var dashboardStats = await _reportRepository.GetDashboardStatisticsAsync();
                var monthlyTrends = await _reportRepository.GetMonthlyTrendsAsync(DateTime.Now.Year);
                var departmentStats = await _reportRepository.GetDepartmentStatisticsAsync();
                var recentActivities = await _reportRepository.GetRecentActivitiesAsync(10);

                dashboardStats.MonthlyTrends = monthlyTrends;
                dashboardStats.DepartmentStats = departmentStats;
                dashboardStats.RecentActivities = recentActivities;

                return View(dashboardStats);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading dashboard: {ex.Message}";
                return View(new DashboardReportModel());
            }
        }

        // GET: Report/Lease
        public async Task<IActionResult> Lease(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetLeaseReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetLeaseReportAsync(new ReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now
                });

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading lease report: {ex.Message}";
                return View(new LeaseReportModel());
            }
        }

        // GET: Report/LeaseSummary
        public async Task<IActionResult> LeaseSummary(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetLeaseSummaryReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetLeaseSummaryReportAsync(new ReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now
                });

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading lease summary report: {ex.Message}";
                return View(new LeaseReportModel());
            }
        }

        // GET: Report/LeaseApproval
        public async Task<IActionResult> LeaseApproval(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetLeaseApprovalReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetLeaseApprovalReportAsync(new ReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now
                });

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading lease approval report: {ex.Message}";
                return View(new LeaseReportModel());
            }
        }

        // GET: Report/LeaseExpiry
        public async Task<IActionResult> LeaseExpiry(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetLeaseExpiryReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetLeaseExpiryReportAsync(new ReportFilterModel
                {
                    ToDate = DateTime.Now.AddMonths(3) // Show leases expiring in next 3 months
                });

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading lease expiry report: {ex.Message}";
                return View(new LeaseReportModel());
            }
        }

        // GET: Report/Employee
        public async Task<IActionResult> Employee(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetEmployeeReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetEmployeeReportAsync(new ReportFilterModel());

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading employee report: {ex.Message}";
                return View(new EmployeeReportModel());
            }
        }

        // GET: Report/Vendor
        public async Task<IActionResult> Vendor(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetVendorReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetVendorReportAsync(new ReportFilterModel());

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading vendor report: {ex.Message}";
                return View(new VendorReportModel());
            }
        }

        // GET: Report/Financial
        public async Task<IActionResult> Financial(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetFinancialReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetFinancialReportAsync(new ReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-6),
                    ToDate = DateTime.Now
                });

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading financial report: {ex.Message}";
                return View(new FinancialReportModel());
            }
        }

        // GET: Report/Approval
        public async Task<IActionResult> Approval(ReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var report = await _reportRepository.GetApprovalReportAsync(filter);
                    return Json(new { success = true, data = report });
                }

                var defaultReport = await _reportRepository.GetApprovalReportAsync(new ReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now
                });

                return View(defaultReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading approval report: {ex.Message}";
                return View(new ApprovalReportModel());
            }
        }

        // POST: Report/Export
        [HttpPost]
        public async Task<IActionResult> Export(string reportType, ReportFilterModel filter)
        {
            try
            {
                BaseReportModel report;
                string fileName;

                switch (reportType.ToLower())
                {
                    case "lease":
                        report = await _reportRepository.GetLeaseReportAsync(filter);
                        fileName = $"Lease_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "leasesummary":
                        report = await _reportRepository.GetLeaseSummaryReportAsync(filter);
                        fileName = $"Lease_Summary_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "leaseapproval":
                        report = await _reportRepository.GetLeaseApprovalReportAsync(filter);
                        fileName = $"Lease_Approval_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "leaseexpiry":
                        report = await _reportRepository.GetLeaseExpiryReportAsync(filter);
                        fileName = $"Lease_Expiry_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "employee":
                        report = await _reportRepository.GetEmployeeReportAsync(filter);
                        fileName = $"Employee_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "vendor":
                        report = await _reportRepository.GetVendorReportAsync(filter);
                        fileName = $"Vendor_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "financial":
                        report = await _reportRepository.GetFinancialReportAsync(filter);
                        fileName = $"Financial_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    case "approval":
                        report = await _reportRepository.GetApprovalReportAsync(filter);
                        fileName = $"Approval_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        break;
                    default:
                        return BadRequest("Invalid report type");
                }

                byte[] fileBytes;
                string contentType;

                switch (filter.ReportFormat.ToLower())
                {
                    case "pdf":
                        fileBytes = await _reportRepository.ExportToPdfAsync(report, "~/Views/Reports/Templates/ReportTemplate.cshtml");
                        contentType = "application/pdf";
                        fileName += ".pdf";
                        break;
                    case "excel":
                        fileBytes = await _reportRepository.ExportToExcelAsync(report);
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        fileName += ".xlsx";
                        break;
                    case "csv":
                        fileBytes = await _reportRepository.ExportToCsvAsync(report);
                        contentType = "text/csv";
                        fileName += ".csv";
                        break;
                    default:
                        return BadRequest("Invalid export format");
                }

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error exporting report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Report/GetMonthlyTrends
        [HttpGet]
        public async Task<IActionResult> GetMonthlyTrends(int year)
        {
            try
            {
                var trends = await _reportRepository.GetMonthlyTrendsAsync(year);
                return Json(new { success = true, data = trends });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Report/GetDepartmentStats
        [HttpGet]
        public async Task<IActionResult> GetDepartmentStats()
        {
            try
            {
                var stats = await _reportRepository.GetDepartmentStatisticsAsync();
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Report/GetRecentActivities
        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int count = 10)
        {
            try
            {
                var activities = await _reportRepository.GetRecentActivitiesAsync(count);
                return Json(new { success = true, data = activities });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Report/GetDashboardStats
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _reportRepository.GetDashboardStatisticsAsync();
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Report/GetDashboardStatsByDateRange
        [HttpGet]
        public async Task<IActionResult> GetDashboardStatsByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var stats = await _reportRepository.GetDashboardStatisticsByDateRangeAsync(fromDate, toDate);
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task LoadViewBagData()
        {
            try
            {
                            // Load employees for filter dropdown
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            ViewBag.Employees = employees.Select(e => new { e.Id, Name = $"{e.Name} ({e.Code})" });

                // Load vendors for filter dropdown
                var vendors = await _vendorRepository.GetAllVendorsAsync();
                ViewBag.Vendors = vendors.Select(v => new { v.Id, v.VendorName });

                // Load lease types for filter dropdown
                var leaseTypes = await _leaseRepository.GetAllLeaseTypesAsync();
                ViewBag.LeaseTypes = leaseTypes.Select(lt => new { lt.Id, lt.Name });

                // Load departments for filter dropdown
                var departments = await _employeeRepository.GetAllDepartmentsAsync();
                ViewBag.Departments = departments.Select(d => new { d.Id, d.Name });

                // Load designations for filter dropdown
                var designations = await _employeeRepository.GetAllDesignationsAsync();
                ViewBag.Designations = designations.Select(d => new { d.Id, d.Name });

                // Status options
                ViewBag.StatusOptions = new[]
                {
                    new { Value = "Active", Text = "Active" },
                    new { Value = "Expired", Text = "Expired" },
                    new { Value = "Terminated", Text = "Terminated" }
                };

                // Approval status options
                ViewBag.ApprovalStatusOptions = new[]
                {
                    new { Value = "1", Text = "Pending" },
                    new { Value = "2", Text = "Approved" },
                    new { Value = "3", Text = "Rejected" }
                };

                // Report format options
                ViewBag.ReportFormatOptions = new[]
                {
                    new { Value = "PDF", Text = "PDF" },
                    new { Value = "Excel", Text = "Excel" },
                    new { Value = "CSV", Text = "CSV" }
                };
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent page from breaking
                Console.WriteLine($"Error loading view bag data: {ex.Message}");
            }
        }
    }
}
