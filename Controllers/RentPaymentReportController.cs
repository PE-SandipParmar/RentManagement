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
    public class RentPaymentReportController : Controller
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILeaseRepository _leaseRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IMonthlyRentPaymentRepository _monthlyRentPaymentRepository;
        private readonly IConfiguration _configuration;

        public RentPaymentReportController(
            IReportRepository reportRepository,
            ILeaseRepository leaseRepository,
            IEmployeeRepository employeeRepository,
            IVendorRepository vendorRepository,
            IMonthlyRentPaymentRepository monthlyRentPaymentRepository,
            IConfiguration configuration)
        {
            _reportRepository = reportRepository;
            _leaseRepository = leaseRepository;
            _employeeRepository = employeeRepository;
            _vendorRepository = vendorRepository;
            _monthlyRentPaymentRepository = monthlyRentPaymentRepository;
            _configuration = configuration;
        }

        // GET: RentPaymentReport
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
                
                var defaultFilter = new RentPaymentReportFilterModel
                {
                    FromDate = DateTime.Now.AddMonths(-1),
                    ToDate = DateTime.Now,
                    SortBy = "PaymentDate",
                    SortOrder = "DESC",
                    FinancialYear = currentFinancialYear,
                    Month = currentMonth,
                    PaymentStatus = null // Default to "All Status"
                };

                var report = await GetRentPaymentReportAsync(defaultFilter);
                
                // Add diagnostic information if no data found
                if (report.TotalRecords == 0)
                {
                    ViewBag.DiagnosticInfo = await GetRentPaymentDiagnosticInfo();
                }
                
                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading Rent Payment report: {ex.Message}";
                return View(new RentPaymentReportModel());
            }
        }

        // GET: RentPaymentReport/Details
        public async Task<IActionResult> Details(RentPaymentReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await GetRentPaymentReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading Rent Payment report details: {ex.Message}";
                return View(new RentPaymentReportModel());
            }
        }

        // GET: RentPaymentReport/Monthly
        public async Task<IActionResult> Monthly(RentPaymentReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await GetRentPaymentMonthlyReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading monthly Rent Payment report: {ex.Message}";
                return View(new RentPaymentReportModel());
            }
        }

        // GET: RentPaymentReport/Vendor
        public async Task<IActionResult> Vendor(RentPaymentReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await GetRentPaymentVendorReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading vendor Rent Payment report: {ex.Message}";
                return View(new RentPaymentReportModel());
            }
        }

        // GET: RentPaymentReport/Employee
        public async Task<IActionResult> Employee(RentPaymentReportFilterModel filter)
        {
            try
            {
                await LoadViewBagData();
                
                var report = await GetRentPaymentEmployeeReportAsync(filter);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, data = report });
                }

                return View(report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading employee Rent Payment report: {ex.Message}";
                return View(new RentPaymentReportModel());
            }
        }

        // GET: RentPaymentReport/Pegging
        public async Task<IActionResult> Pegging(int paymentId, string paymentType)
        {
            try
            {
                var pegging = await GetRentPaymentPeggingAsync(paymentId, paymentType);
                return View(pegging);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading pegging details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: RentPaymentReport/Charts
        public async Task<IActionResult> Charts(RentPaymentReportFilterModel filter)
        {
            try
            {
                var chartData = await GetRentPaymentChartDataAsync(filter);
                return Json(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: RentPaymentReport/Export
        [HttpPost]
        public async Task<IActionResult> Export(RentPaymentReportFilterModel filter, string format = "PDF")
        {
            try
            {
                // Set page to 0 for export to get all data
                filter.Page = 0;
                filter.PageSize = 10000; // Large page size for export
                
                var report = await GetRentPaymentReportAsync(filter);
                var options = new RentPaymentReportExportOptions
                {
                    Format = format,
                    IncludeSummary = true,
                    IncludeCharts = true,
                    IncludeFilters = true,
                    CompanyName = "Food Safety and Standards Authority of India",
                    ReportTitle = "Rent Payment Report"
                };

                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format.ToUpper())
                {
                    case "PDF":
                        fileBytes = await ExportRentPaymentReportToPdfAsync(report, options);
                        fileName = $"Rent_Payment_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                        contentType = "text/html";
                        break;

                    case "EXCEL":
                        fileBytes = await ExportRentPaymentReportToExcelAsync(report, options);
                        fileName = $"Rent_Payment_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;

                    case "CSV":
                        var csvContent = await ExportRentPaymentReportToCsvAsync(report, options);
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                        fileName = $"Rent_Payment_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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

        // AJAX: Get Rent Payment Statistics
        [HttpGet]
        public async Task<IActionResult> GetRentPaymentStatistics(RentPaymentReportFilterModel filter)
        {
            try
            {
                var report = await GetRentPaymentReportAsync(filter);
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
                var testResults = await TestBasicRentPaymentQueryAsync();
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

                return Json(new
                {
                    success = true,
                    employees = employees.Select(e => new { e.Id, e.Code, e.Name }),
                    vendors = vendors.Select(v => new { v.Id, v.VendorCode, v.VendorName, v.PANNumber })
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
                        mrp.Id,
                        mrp.Amount,
                        mrp.PaymentDate,
                        mrp.PaymentMonth,
                        e.Code as EmployeeCode,
                        e.Name as EmployeeName,
                        v.VendorCode,
                        v.VendorName,
                        v.PANNumber
                    FROM MonthlyRentPayments mrp
                    LEFT JOIN Employees e ON mrp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON mrp.VendorId = v.Id
                    ORDER BY mrp.PaymentDate DESC";

                var rawData = await connection.QueryAsync(sql);
                
                return Json(new { 
                    Success = true, 
                    Data = rawData,
                    Message = "Raw data from MonthlyRentPayments table"
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
                var diagnosticInfo = await GetRentPaymentDiagnosticInfoAsync();
                return Json(diagnosticInfo);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        #region Private Methods

        private async Task<RentPaymentReportModel> GetRentPaymentReportAsync(RentPaymentReportFilterModel filter)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                
                var whereClause = BuildWhereClause(filter);
                var orderClause = BuildOrderClause(filter);
                var paginationClause = BuildPaginationClause(filter);

                var sql = $@"
                    SELECT 
                        ROW_NUMBER() OVER ({orderClause}) as SlNo,
                        mrp.Id as PaymentId,
                        CONCAT('CN/', FORMAT(mrp.Id, '000000'), '/25-26') as PaymentNumber,
                        CONCAT('CN/', FORMAT(mrp.Id, '000000'), '/25-26') as TransactionNumber,
                        v.VendorName,
                        COALESCE(v.BankName, 'N/A') as BankName,
                        COALESCE(v.AccountNumber, 'N/A') as AccountNumber,
                        v.PANNumber as PAN,
                        CONCAT('R', FORMAT(mrp.Id, '00000'), '-52') as InvoiceNumber,
                        mrp.PaymentDate as InvoiceDate,
                        '' as POAdvanceNumber,
                        COALESCE(v.IFSCCode, 'N/A') as IFSC,
                        COALESCE(v.GSTNumber, 'N/A') as GSTN,
                        mrp.Amount,
                        0 as Adjustment,
                        0 as Recoveries,
                        mrp.Amount as Payable,
                        'Project & CoA Head' as ProjectCoAHead,
                        CONCAT('Rent Payable for the Month of ', mrp.PaymentMonth, ' on behalf of ', CONCAT('R', FORMAT(mrp.Id, '00000'), '-52'), ' : ', e.Name) as Remarks,
                        e.Name as EmployeeName,
                        CONCAT('R', FORMAT(mrp.Id, '00000'), '-52') as LeaseReference,
                        'Rent' as PaymentType,
                        'Posted' as PaymentStatus,
                        mrp.PaymentDate,
                        mrp.PaymentMonth as Month,
                        YEAR(mrp.PaymentDate) as Year,
                        CONCAT(YEAR(mrp.PaymentDate), '-', RIGHT(YEAR(mrp.PaymentDate) + 1, 2)) as FinancialYear,
                        0 as TDSAmount,
                        mrp.Amount as NetPayableAmount,
                        mrp.CreatedBy,
                        mrp.CreatedDate
                    FROM MonthlyRentPayments mrp
                    LEFT JOIN Employees e ON mrp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON mrp.VendorId = v.Id
                    {whereClause}
                    {orderClause}
                    {paginationClause}";

                var items = await connection.QueryAsync<RentPaymentItem>(sql);

                // Get total count
                var countSql = $@"
                    SELECT COUNT(*)
                    FROM MonthlyRentPayments mrp
                    LEFT JOIN Employees e ON mrp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON mrp.VendorId = v.Id
                    {whereClause}";

                var totalRecords = await connection.QuerySingleAsync<int>(countSql);

                // Calculate summary
                var summary = await CalculateRentPaymentSummaryAsync(filter);

                return new RentPaymentReportModel
                {
                    ReportTitle = "Rent Payment Report",
                    OrganizationName = "Food Safety and Standards Authority of India",
                    OrganizationAddress = "FDA Bhawan, Kotla Marg, near Bal Bhavan, New Delhi - 110002",
                    VoucherNumber = $"RQBP1/001038/25-26 dt : {DateTime.Now:dd/MM/yyyy}",
                    ChequeNumber = $"289255 dt. {DateTime.Now:dd/MM/yyyy}",
                    PaymentDate = DateTime.Now,
                    PaymentReference = "RENT FOR THE MONTH OF JULY 2025 ALONG WITH SOME JUNE 2025 ARREARS",
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = User.Identity?.Name ?? "System",
                    Filter = filter,
                    PaymentItems = items.ToList(),
                    Summary = summary,
                    TotalRecords = totalRecords,
                    ReportType = "RentPayment",
                    ReportPeriod = $"{filter.FromDate?.ToString("MMM yyyy")} - {filter.ToDate?.ToString("MMM yyyy")}"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating rent payment report: {ex.Message}", ex);
            }
        }

        private async Task<RentPaymentReportModel> GetRentPaymentMonthlyReportAsync(RentPaymentReportFilterModel filter)
        {
            // Implementation for monthly report
            return await GetRentPaymentReportAsync(filter);
        }

        private async Task<RentPaymentReportModel> GetRentPaymentVendorReportAsync(RentPaymentReportFilterModel filter)
        {
            // Implementation for vendor report
            return await GetRentPaymentReportAsync(filter);
        }

        private async Task<RentPaymentReportModel> GetRentPaymentEmployeeReportAsync(RentPaymentReportFilterModel filter)
        {
            // Implementation for employee report
            return await GetRentPaymentReportAsync(filter);
        }

        private async Task<RentPaymentPeggingModel> GetRentPaymentPeggingAsync(int paymentId, string paymentType)
        {
            // Implementation for pegging details
            return new RentPaymentPeggingModel();
        }

        private async Task<RentPaymentChartData> GetRentPaymentChartDataAsync(RentPaymentReportFilterModel filter)
        {
            // Implementation for chart data
            return new RentPaymentChartData();
        }

        private async Task<RentPaymentSummary> CalculateRentPaymentSummaryAsync(RentPaymentReportFilterModel filter)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                
                var whereClause = BuildWhereClause(filter);

                var sql = $@"
                    SELECT 
                        COUNT(*) as TotalPayments,
                        SUM(mrp.Amount) as TotalAmount,
                        0 as TotalAdjustment,
                        0 as TotalRecoveries,
                        SUM(mrp.Amount) as TotalPayable,
                        0 as TotalTDSAmount,
                        SUM(mrp.Amount) as TotalNetPayable,
                        COUNT(DISTINCT v.Id) as UniqueVendors,
                        COUNT(DISTINCT e.Id) as UniqueEmployees,
                        COUNT(DISTINCT v.BankName) as UniqueBanks
                    FROM MonthlyRentPayments mrp
                    LEFT JOIN Employees e ON mrp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON mrp.VendorId = v.Id
                    {whereClause}";

                var result = await connection.QuerySingleAsync(sql);
                
                return new RentPaymentSummary
                {
                    TotalPayments = result.TotalPayments,
                    TotalAmount = result.TotalAmount ?? 0,
                    TotalAdjustment = result.TotalAdjustment ?? 0,
                    TotalRecoveries = result.TotalRecoveries ?? 0,
                    TotalPayable = result.TotalPayable ?? 0,
                    TotalTDSAmount = result.TotalTDSAmount ?? 0,
                    TotalNetPayable = result.TotalNetPayable ?? 0,
                    UniqueVendors = result.UniqueVendors,
                    UniqueEmployees = result.UniqueEmployees,
                    UniqueBanks = result.UniqueBanks
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating rent payment summary: {ex.Message}", ex);
            }
        }

        private string BuildWhereClause(RentPaymentReportFilterModel filter)
        {
            var conditions = new List<string>();

            if (filter.FromDate.HasValue)
                conditions.Add($"mrp.PaymentDate >= '{filter.FromDate.Value:yyyy-MM-dd}'");

            if (filter.ToDate.HasValue)
                conditions.Add($"mrp.PaymentDate <= '{filter.ToDate.Value:yyyy-MM-dd}'");

            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                conditions.Add($"e.Code LIKE '%{filter.EmployeeCode}%'");

            if (!string.IsNullOrEmpty(filter.EmployeeName))
                conditions.Add($"e.Name LIKE '%{filter.EmployeeName}%'");

            if (!string.IsNullOrEmpty(filter.VendorCode))
                conditions.Add($"v.VendorCode LIKE '%{filter.VendorCode}%'");

            if (!string.IsNullOrEmpty(filter.VendorName))
                conditions.Add($"v.VendorName LIKE '%{filter.VendorName}%'");

            if (!string.IsNullOrEmpty(filter.PAN))
                conditions.Add($"v.PANNumber LIKE '%{filter.PAN}%'");

            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                conditions.Add($"mrp.PaymentStatus = '{filter.PaymentStatus}'");

            if (!string.IsNullOrEmpty(filter.Month))
                conditions.Add($"mrp.PaymentMonth = '{filter.Month}'");

            if (!string.IsNullOrEmpty(filter.BankName))
                conditions.Add($"v.BankName LIKE '%{filter.BankName}%'");

            if (filter.MinAmount.HasValue)
                conditions.Add($"mrp.Amount >= {filter.MinAmount.Value}");

            if (filter.MaxAmount.HasValue)
                conditions.Add($"mrp.Amount <= {filter.MaxAmount.Value}");

            if (!filter.IncludeZeroAmounts)
                conditions.Add("mrp.Amount > 0");

            return conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
        }

        private string BuildOrderClause(RentPaymentReportFilterModel filter)
        {
            var sortBy = filter.SortBy ?? "PaymentDate";
            var sortOrder = filter.SortOrder ?? "DESC";

            return $"ORDER BY mrp.{sortBy} {sortOrder}";
        }

        private string BuildPaginationClause(RentPaymentReportFilterModel filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var offset = (filter.Page - 1) * filter.PageSize;
            return $"OFFSET {offset} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";
        }

        private async Task<byte[]> ExportRentPaymentReportToPdfAsync(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            // Implementation for PDF export
            var html = GenerateRentPaymentReportHtml(report, options);
            return System.Text.Encoding.UTF8.GetBytes(html);
        }

        private async Task<byte[]> ExportRentPaymentReportToExcelAsync(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            // Implementation for Excel export
            return new byte[0];
        }

        private async Task<string> ExportRentPaymentReportToCsvAsync(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            // Implementation for CSV export
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Sl No,Payment No,Transaction No,Vendor Name,Bank Name,Account No,PAN,Invoice No,Amount,Payable,Employee Name,Remarks");
            
            foreach (var item in report.PaymentItems)
            {
                csv.AppendLine($"{item.SlNo},{item.PaymentNumber},{item.TransactionNumber},{item.VendorName},{item.BankName},{item.AccountNumber},{item.PAN},{item.InvoiceNumber},{item.Amount},{item.Payable},{item.EmployeeName},{item.Remarks}");
            }
            
            return csv.ToString();
        }

        private string GenerateRentPaymentReportHtml(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            // Implementation for HTML generation
            return "<html><body><h1>Rent Payment Report</h1></body></html>";
        }

        private async Task<object> TestBasicRentPaymentQueryAsync()
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                
                var sql = @"
                    SELECT TOP 5
                        mrp.Id,
                        mrp.Amount,
                        mrp.PaymentDate,
                        mrp.PaymentMonth,
                        e.Code as EmployeeCode,
                        e.Name as EmployeeName,
                        v.VendorCode,
                        v.VendorName,
                        v.PANNumber
                    FROM MonthlyRentPayments mrp
                    LEFT JOIN Employees e ON mrp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON mrp.VendorId = v.Id
                    ORDER BY mrp.PaymentDate DESC";

                var rawData = await connection.QueryAsync(sql);
                
                return new { 
                    Success = true, 
                    Data = rawData,
                    Message = "Test query executed successfully"
                };
            }
            catch (Exception ex)
            {
                return new { 
                    Success = false, 
                    Error = ex.Message, 
                    StackTrace = ex.StackTrace 
                };
            }
        }

        private async Task<object> GetRentPaymentDiagnosticInfoAsync()
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                
                var totalPayments = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM MonthlyRentPayments");
                var totalVendors = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM Vendors");
                var totalEmployees = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM Employees");
                
                return new
                {
                    HasData = totalPayments > 0,
                    TotalPayments = totalPayments,
                    TotalVendors = totalVendors,
                    TotalEmployees = totalEmployees,
                    HasRentData = totalPayments > 0
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = ex.Message,
                    HasData = false,
                    HasRentData = false
                };
            }
        }

        private async Task<object> GetRentPaymentDiagnosticInfo()
        {
            try
            {
                return await GetRentPaymentDiagnosticInfoAsync();
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = ex.Message,
                    HasData = false,
                    HasRentData = false
                };
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

                // Payment status options
                ViewBag.PaymentStatusOptions = new[]
                {
                    new { Value = "Pending", Text = "Pending" },
                    new { Value = "Paid", Text = "Paid" },
                    new { Value = "Overdue", Text = "Overdue" },
                    new { Value = "Cancelled", Text = "Cancelled" },
                    new { Value = "Posted", Text = "Posted" }
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
                    new { Value = "Amount", Text = "Amount" },
                    new { Value = "PaymentNumber", Text = "Payment Number" }
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

        #endregion
    }
}
