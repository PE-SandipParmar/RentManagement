using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;

namespace RentManagement.Controllers
{
    [Authorize]
    public class TDSReportController : Controller
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILeaseRepository _leaseRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IMonthlyRentPaymentRepository _monthlyRentPaymentRepository;
        private readonly IBrokeragePaymentRepository _brokeragePaymentRepository;

        public TDSReportController(
            IReportRepository reportRepository,
            ILeaseRepository leaseRepository,
            IEmployeeRepository employeeRepository,
            IVendorRepository vendorRepository,
            IMonthlyRentPaymentRepository monthlyRentPaymentRepository,
            IBrokeragePaymentRepository brokeragePaymentRepository)
        {
            _reportRepository = reportRepository;
            _leaseRepository = leaseRepository;
            _employeeRepository = employeeRepository;
            _vendorRepository = vendorRepository;
            _monthlyRentPaymentRepository = monthlyRentPaymentRepository;
            _brokeragePaymentRepository = brokeragePaymentRepository;
        }

        // GET: TDSReport
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
                
                var defaultFilter = new TDSReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now,
                    SortBy = "PaymentDate",
                    SortOrder = "DESC",
                    FinancialYear = currentFinancialYear,
                    Month = currentMonth,
                    TDSApplicableId =null, // Default to "All TDS Types"
                    PaymentStatus = null // Default to "All Status"
                };

                var report = await _reportRepository.GetTDSReportAsync(defaultFilter);
                
                // Add diagnostic information if no data found
                if (report.TotalRecords == 0)
                {
                    ViewBag.DiagnosticInfo = await GetTDSDiagnosticInfo();
                }
                
                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading TDS report: {ex.Message}";
                return View(new TDSReportModel());
            }
        }

        // GET: TDSReport/Details
        public async Task<IActionResult> Details(TDSReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading TDS report details: {ex.Message}";
                return View(new TDSReportModel());
            }
        }

        // GET: TDSReport/Monthly
        public async Task<IActionResult> Monthly(TDSReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSMonthlyReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading monthly TDS report: {ex.Message}";
                return View(new TDSReportModel());
            }
        }

        // GET: TDSReport/Vendor
        public async Task<IActionResult> Vendor(TDSReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSVendorReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading vendor TDS report: {ex.Message}";
                return View(new TDSReportModel());
            }
        }

        // GET: TDSReport/Employee
        public async Task<IActionResult> Employee(TDSReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await _reportRepository.GetTDSEmployeeReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading employee TDS report: {ex.Message}";
                return View(new TDSReportModel());
            }
        }

        // GET: TDSReport/Pegging
        public async Task<IActionResult> Pegging(int paymentId, string paymentType)
        {
            try
            {
                var pegging = await _reportRepository.GetTDSPeggingAsync(paymentId, paymentType);
                return View(pegging);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading pegging details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: TDSReport/Charts
        public async Task<IActionResult> Charts(TDSReportFilterModel filter)
        {
            try
            {
                var chartData = await _reportRepository.GetTDSChartDataAsync(filter);
                return Json(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: TDSReport/Export
        [HttpPost]
        public async Task<IActionResult> Export(TDSReportFilterModel filter, string format = "PDF")
        {
            try
            {
                // Set page to 0 for export to get all data
                filter.Page = 0;
                filter.PageSize = 10000; // Large page size for export
                
                var report = await _reportRepository.GetTDSReportAsync(filter);
                var options = new TDSReportExportOptions
                {
                    Format = format,
                    IncludeSummary = true,
                    IncludeCharts = true,
                    IncludeFilters = true,
                    CompanyName = "Your Company Name",
                    ReportTitle = "TDS Report - Lease Payable"
                };

                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format.ToUpper())
                {
                    case "PDF":
                        fileBytes = await _reportRepository.ExportTDSReportToPdfAsync(report, options);
                        fileName = $"TDS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                        contentType = "text/html";
                        break;

                    case "EXCEL":
                        fileBytes = await _reportRepository.ExportTDSReportToExcelAsync(report, options);
                        fileName = $"TDS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case "CSV":
                        var csvContent = await _reportRepository.ExportTDSReportToCsvAsync(report, options);
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                        fileName = $"TDS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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

        // AJAX: Get TDS Statistics
        [HttpGet]
        public async Task<IActionResult> GetTDSStatistics(TDSReportFilterModel filter)
        {
            try
            {
                var report = await _reportRepository.GetTDSReportAsync(filter);
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
                var testResults = await _reportRepository.TestBasicTDSQueryAsync();
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
                var tdsApplicable = await _monthlyRentPaymentRepository.GetTdsApplicableAsync();

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
                var tdsApplicable = await _monthlyRentPaymentRepository.GetTdsApplicableAsync();
                ViewBag.TDSApplicable = tdsApplicable.Select(t => new { t.Id, Name = $"{t.Name} ({t.Rate}%)" });

                // Payment status options
                ViewBag.PaymentStatusOptions = new[]
                {
                    new { Value = "Pending", Text = "Pending" },
                    new { Value = "Paid", Text = "Paid" },
                    new { Value = "Overdue", Text = "Overdue" },
                    new { Value = "Cancelled", Text = "Cancelled" }
                };

                // Payment type options
                ViewBag.PaymentTypeOptions = new[]
                {
                    new { Value = "MonthlyRent", Text = "Monthly Rent" },
                    new { Value = "Brokerage", Text = "Brokerage" },
                    new { Value = "SecurityDeposit", Text = "Security Deposit" }
                };

                // Financial year options - last 4 years
                var currentYear = DateTime.Now.Year;
                var financialYears = new List<object>();
                
                for (int i = 3; i >= 0; i--)
                {
                    var year = currentYear - i;
                    var nextYear = year + 1;
                    // Use the same logic as SQL: RIGHT('0' + CAST(YEAR + 1 AS VARCHAR(2)), 2)
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

        private async Task<object> GetTDSDiagnosticInfo()
        {
            try
            {
                return await _reportRepository.GetTDSDiagnosticInfoAsync();
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
