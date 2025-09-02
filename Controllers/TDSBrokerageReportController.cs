using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;

namespace RentManagement.Controllers
{
    [Authorize]
    public class TDSBrokerageReportController : Controller
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILeaseRepository _leaseRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IBrokeragePaymentRepository _brokeragePaymentRepository;
        private readonly IConfiguration _configuration;

        public TDSBrokerageReportController(
            IReportRepository reportRepository,
            ILeaseRepository leaseRepository,
            IEmployeeRepository employeeRepository,
            IVendorRepository vendorRepository,
            IBrokeragePaymentRepository brokeragePaymentRepository,
            IConfiguration configuration)
        {
            _reportRepository = reportRepository;
            _leaseRepository = leaseRepository;
            _employeeRepository = employeeRepository;
            _vendorRepository = vendorRepository;
            _brokeragePaymentRepository = brokeragePaymentRepository;
            _configuration = configuration;
        }

        // GET: TDSBrokerageReport
        public async Task<IActionResult> Index()
        {
            try
            {
                await LoadViewBagData();
                
                var currentYear = DateTime.Now.Year;
                var currentMonth = DateTime.Now.ToString("MMMM");
                var nextYear = currentYear + 1;
                var nextYearStr = nextYear.ToString();
                if (nextYearStr.Length > 2)
                {
                    nextYearStr = nextYearStr.Substring(nextYearStr.Length - 2);
                }
                else
                {
                    nextYearStr = nextYearStr.PadLeft(2, '0');
                }
                var currentFinancialYear = $"{currentYear}-{nextYearStr}";
                
                var defaultFilter = new TDSBrokerageReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now,
                    SortBy = "PaymentDate",
                    SortOrder = "DESC",
                    FinancialYear = currentFinancialYear,
                    Month = currentMonth,
                    TDSApplicableId = null, // Default to "All TDS Types"
                    PaymentStatus = null // Default to "All Status"
                };

                var report = await _reportRepository.GetTDSBrokerageReportAsync(defaultFilter);
                
                // Add diagnostic information if no data found
                if (report.TotalRecords == 0)
                {
                    ViewBag.DiagnosticInfo = await GetTDSBrokerageDiagnosticInfo();
                }
                
                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading TDS Brokerage report: {ex.Message}";
                return View(new TDSBrokerageReportModel());
            }
        }

        // GET: TDSBrokerageReport/Details
        public async Task<IActionResult> Details(TDSBrokerageReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSBrokerageReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading TDS Brokerage report details: {ex.Message}";
                return View(new TDSBrokerageReportModel());
            }
        }

        // GET: TDSBrokerageReport/Monthly
        public async Task<IActionResult> Monthly(TDSBrokerageReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSBrokerageMonthlyReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading monthly TDS Brokerage report: {ex.Message}";
                return View(new TDSBrokerageReportModel());
            }
        }

        // GET: TDSBrokerageReport/Vendor
        public async Task<IActionResult> Vendor(TDSBrokerageReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSBrokerageVendorReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading vendor TDS Brokerage report: {ex.Message}";
                return View(new TDSBrokerageReportModel());
            }
        }

        // GET: TDSBrokerageReport/Employee
        public async Task<IActionResult> Employee(TDSBrokerageReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSBrokerageEmployeeReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading employee TDS Brokerage report: {ex.Message}";
                return View(new TDSBrokerageReportModel());
            }
        }

        // GET: TDSBrokerageReport/Pegging
        public async Task<IActionResult> Pegging(int paymentId, string paymentType)
        {
            try
            {
                var pegging = await _reportRepository.GetTDSBrokeragePeggingAsync(paymentId, paymentType);
                return View(pegging);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading pegging details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: TDSBrokerageReport/Charts
        public async Task<IActionResult> Charts(TDSBrokerageReportFilterModel filter)
        {
            try
            {
                var chartData = await _reportRepository.GetTDSBrokerageChartDataAsync(filter);
                return Json(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: TDSBrokerageReport/Export
        [HttpPost]
        public async Task<IActionResult> Export(TDSBrokerageReportFilterModel filter, string format = "PDF")
        {
            try
            {
                // Set page to 0 for export to get all data
                filter.Page = 0;
                filter.PageSize = 10000; // Large page size for export
                
                var report = await _reportRepository.GetTDSBrokerageReportAsync(filter);
                var options = new TDSBrokerageReportExportOptions
                {
                    Format = format,
                    IncludeSummary = true,
                    IncludeCharts = true,
                    IncludeFilters = true,
                    CompanyName = "Your Company Name",
                    ReportTitle = "TDS Brokerage Report - Lease Brokerage"
                };

                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format.ToUpper())
                {
                    case "PDF":
                        fileBytes = await _reportRepository.ExportTDSBrokerageReportToPdfAsync(report, options);
                        fileName = $"TDS_Brokerage_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                        contentType = "text/html";
                        break;

                    case "EXCEL":
                        fileBytes = await _reportRepository.ExportTDSBrokerageReportToExcelAsync(report, options);
                        fileName = $"TDS_Brokerage_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case "CSV":
                        var csvContent = await _reportRepository.ExportTDSBrokerageReportToCsvAsync(report, options);
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                        fileName = $"TDS_Brokerage_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        contentType = "text/csv";
                        break;

                    default:
                        throw new ArgumentException("Unsupported export format");
                }

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error exporting report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // AJAX: Get TDS Brokerage Statistics
        [HttpGet]
        public async Task<IActionResult> GetTDSBrokerageStatistics(TDSBrokerageReportFilterModel filter)
        {
            try
            {
                var report = await _reportRepository.GetTDSBrokerageReportAsync(filter);
                return Json(new { success = true, data = report.Summary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Test Basic Query
        [HttpGet]
        public async Task<IActionResult> TestBasicQuery()
        {
            try
            {
                var testResults = await _reportRepository.TestBasicTDSBrokerageQueryAsync();
                return Json(new { success = true, data = testResults });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // AJAX: Get Filter Options
        [HttpGet]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var employees = await _employeeRepository.GetAllEmployeesAsync();
                var vendors = await _vendorRepository.GetAllVendorsAsync();
                var tdsApplicable = await _brokeragePaymentRepository.GetTdsApplicableAsync();

                return Json(new
                {
                    success = true,
                    employees = employees.Select(e => new { e.Id, e.Code, e.Name }),
                    vendors = vendors.Select(v => new { v.Id, v.VendorCode, v.VendorName, v.PANNumber }),
                    tdsApplicable = tdsApplicable.Select(t => new { t.Id, t.Name, t.Rate })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("RawData")]
        public async Task<IActionResult> RawData()
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                
                var sql = @"
                    SELECT TOP 10
                        bp.Id,
                        bp.BrokerageAmount,
                        bp.TDSRate,
                        bp.TDSAmount,
                        bp.NetPayableAmount,
                        bp.PaymentDate,
                        bp.PaymentMonth,
                        e.Code as EmployeeCode,
                        e.Name as EmployeeName,
                        v.VendorCode,
                        v.VendorName
                    FROM BrokeragePayment bp
                    LEFT JOIN Employees e ON bp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON bp.VendorId = v.Id
                    WHERE bp.TDSAmount > 0
                    ORDER BY bp.PaymentDate DESC";

                var rawData = await connection.QueryAsync(sql);
                
                return Json(new { 
                    Success = true, 
                    Data = rawData,
                    Message = "Raw data from BrokeragePayment table"
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    Success = false, 
                    Error = ex.Message, 
                    StackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet]
        [Route("Diagnostic")]
        public async Task<IActionResult> Diagnostic()
        {
            try
            {
                var diagnosticInfo = await _reportRepository.GetTDSBrokerageDiagnosticInfoAsync();
                return Json(diagnosticInfo);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
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
                ViewBag.Vendors = vendors.Select(v => new { v.Id, Name = $"{v.VendorName} ({v.VendorCode})" });

                // Load TDS applicable options
                var tdsApplicable = await _brokeragePaymentRepository.GetTdsApplicableAsync();
                ViewBag.TDSApplicable = tdsApplicable.Select(t => new { t.Id, Name = $"{t.Name} ({t.Rate}%)" });

                // Payment status options
                ViewBag.PaymentStatusOptions = new[]
                {
                    new { Value = "Pending", Text = "Pending" },
                    new { Value = "Paid", Text = "Paid" },
                    new { Value = "Overdue", Text = "Overdue" },
                    new { Value = "Cancelled", Text = "Cancelled" }
                };

                // Financial year options - last 4 years
                var currentYear = DateTime.Now.Year;
                var financialYears = new List<object>();
                
                for (int i = 3; i >= 0; i--)
                {
                    var year = currentYear - i;
                    var nextYear = year + 1;
                    var nextYearStr = nextYear.ToString();
                    if (nextYearStr.Length > 2)
                    {
                        nextYearStr = nextYearStr.Substring(nextYearStr.Length - 2);
                    }
                    else
                    {
                        nextYearStr = nextYearStr.PadLeft(2, '0');
                    }
                    var financialYear = $"{year}-{nextYearStr}";
                    financialYears.Add(new { Value = financialYear, Text = financialYear });
                }
                
                ViewBag.FinancialYearOptions = financialYears;

                // Month options
                var currentMonth = DateTime.Now.ToString("MMMM");
                ViewBag.MonthOptions = new[]
                {
                    new { Value = "January", Text = "January" },
                    new { Value = "February", Text = "February" },
                    new { Value = "March", Text = "March" },
                    new { Value = "April", Text = "April" },
                    new { Value = "May", Text = "May" },
                    new { Value = "June", Text = "June" },
                    new { Value = "July", Text = "July" },
                    new { Value = "August", Text = "August" },
                    new { Value = "September", Text = "September" },
                    new { Value = "October", Text = "October" },
                    new { Value = "November", Text = "November" },
                    new { Value = "December", Text = "December" }
                };

                // Sort options
                ViewBag.SortOptions = new[]
                {
                    new { Value = "PaymentDate", Text = "Payment Date" },
                    new { Value = "EmployeeName", Text = "Employee Name" },
                    new { Value = "VendorName", Text = "Vendor Name" },
                    new { Value = "TDSAmount", Text = "TDS Amount" },
                    new { Value = "VoucherNumber", Text = "Voucher Number" }
                };

                // Export format options
                ViewBag.ExportFormatOptions = new[]
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

        private async Task<object> GetTDSBrokerageDiagnosticInfo()
        {
            try
            {
                return await _reportRepository.GetTDSBrokerageDiagnosticInfoAsync();
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = ex.Message,
                    HasData = false,
                    HasTDSData = false
                };
            }
        }
    }
}
