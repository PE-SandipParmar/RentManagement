using Dapper;
using RentManagement.Models;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace RentManagement.Data
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);

        // Lease Reports
        public async Task<LeaseReportModel> GetLeaseReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = BuildLeaseReportQuery(filter);
            var parameters = BuildReportParameters(filter);

            var leases = await connection.QueryAsync<LeaseReportItem>(sql, parameters);
            var leaseList = leases.ToList();

            return new LeaseReportModel
            {
                ReportTitle = "Lease Report",
                ReportType = "Lease",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                Leases = leaseList,
                TotalRecords = leaseList.Count,
                TotalMonthlyRent = leaseList.Sum(l => l.MonthlyRentPayable ?? 0),
                TotalRentAmount = leaseList.Sum(l => l.RentAmount ?? 0),
                TotalMaintenancePayment = leaseList.Sum(l => l.MaintenancePayment ?? 0),
                ActiveLeases = leaseList.Count(l => l.Status == "Active"),
                ExpiredLeases = leaseList.Count(l => l.Status == "Expired"),
                PendingApprovals = leaseList.Count(l => l.ApprovalStatus == "Pending")
            };
        }

        public async Task<LeaseReportModel> GetLeaseSummaryReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    COUNT(*) as TotalLeases,
                    COUNT(CASE WHEN Status = 'Active' THEN 1 END) as ActiveLeases,
                    COUNT(CASE WHEN Status = 'Expired' THEN 1 END) as ExpiredLeases,
                    COUNT(CASE WHEN ApprovalStatus = 1 THEN 1 END) as PendingApprovals,
                    SUM(MonthlyRentPayable) as TotalMonthlyRent,
                    SUM(RentAmount) as TotalRentAmount,
                    SUM(MaintenancePayment) as TotalMaintenancePayment
                FROM Leases l
                WHERE l.IsActiveRecord = 1
                AND (@FromDate IS NULL OR l.CreatedAt >= @FromDate)
                AND (@ToDate IS NULL OR l.CreatedAt <= @ToDate)
                AND (@EmployeeId IS NULL OR l.EmployeeId = @EmployeeId)
                AND (@VendorId IS NULL OR l.VendorId = @VendorId)
                AND (@LeaseTypeId IS NULL OR l.LeaseTypeId = @LeaseTypeId)
                AND (@Status IS NULL OR l.Status = @Status)
                AND (@ApprovalStatus IS NULL OR l.ApprovalStatus = @ApprovalStatus)";

            var parameters = BuildReportParameters(filter);
            var summary = await connection.QueryFirstOrDefaultAsync(sql, parameters);

            return new LeaseReportModel
            {
                ReportTitle = "Lease Summary Report",
                ReportType = "LeaseSummary",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                TotalRecords = summary?.TotalLeases ?? 0,
                TotalMonthlyRent = summary?.TotalMonthlyRent ?? 0,
                TotalRentAmount = summary?.TotalRentAmount ?? 0,
                TotalMaintenancePayment = summary?.TotalMaintenancePayment ?? 0,
                ActiveLeases = summary?.ActiveLeases ?? 0,
                ExpiredLeases = summary?.ExpiredLeases ?? 0,
                PendingApprovals = summary?.PendingApprovals ?? 0
            };
        }

        public async Task<LeaseReportModel> GetLeaseApprovalReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    l.Id,
                    l.RefNo,
                    e.Name as EmployeeName,
                    v.VendorName,
                    lt.Name as LeaseTypeName,
                    l.MonthlyRentPayable,
                    l.RentAmount,
                    l.MaintenancePayment,
                    l.FromDate,
                    l.EndDate,
                    l.Status,
                    CASE 
                        WHEN l.ApprovalStatus = 1 THEN 'Pending'
                        WHEN l.ApprovalStatus = 2 THEN 'Approved'
                        WHEN l.ApprovalStatus = 3 THEN 'Rejected'
                        ELSE 'Unknown'
                    END as ApprovalStatus,
                    l.CreatedAt,
                    l.MakerUserName,
                    l.CheckerUserName
                FROM Leases l
                LEFT JOIN Employees e ON l.EmployeeId = e.Id
                LEFT JOIN Vendors v ON l.VendorId = v.Id
                LEFT JOIN LeaseTypes lt ON l.LeaseTypeId = lt.Id
                WHERE l.IsActiveRecord = 1
                AND (@FromDate IS NULL OR l.CreatedAt >= @FromDate)
                AND (@ToDate IS NULL OR l.CreatedAt <= @ToDate)
                AND (@ApprovalStatus IS NULL OR l.ApprovalStatus = @ApprovalStatus)
                ORDER BY l.CreatedAt DESC";

            var parameters = BuildReportParameters(filter);
            var leases = await connection.QueryAsync<LeaseReportItem>(sql, parameters);
            var leaseList = leases.ToList();

            return new LeaseReportModel
            {
                ReportTitle = "Lease Approval Report",
                ReportType = "LeaseApproval",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                Leases = leaseList,
                TotalRecords = leaseList.Count,
                PendingApprovals = leaseList.Count(l => l.ApprovalStatus == "Pending")
            };
        }

        public async Task<LeaseReportModel> GetLeaseExpiryReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    l.Id,
                    l.RefNo,
                    e.Name as EmployeeName,
                    v.VendorName,
                    lt.Name as LeaseTypeName,
                    l.MonthlyRentPayable,
                    l.RentAmount,
                    l.MaintenancePayment,
                    l.FromDate,
                    l.EndDate,
                    l.Status,
                    CASE 
                        WHEN l.ApprovalStatus = 1 THEN 'Pending'
                        WHEN l.ApprovalStatus = 2 THEN 'Approved'
                        WHEN l.ApprovalStatus = 3 THEN 'Rejected'
                        ELSE 'Unknown'
                    END as ApprovalStatus,
                    l.CreatedAt,
                    l.MakerUserName,
                    l.CheckerUserName
                FROM Leases l
                LEFT JOIN Employees e ON l.EmployeeId = e.Id
                LEFT JOIN Vendors v ON l.VendorId = v.Id
                LEFT JOIN LeaseTypes lt ON l.LeaseTypeId = lt.Id
                WHERE l.IsActiveRecord = 1
                AND l.EndDate IS NOT NULL
                AND l.EndDate <= @ToDate
                AND l.Status = 'Active'
                ORDER BY l.EndDate ASC";

            var parameters = BuildReportParameters(filter);
            var leases = await connection.QueryAsync<LeaseReportItem>(sql, parameters);
            var leaseList = leases.ToList();

            return new LeaseReportModel
            {
                ReportTitle = "Lease Expiry Report",
                ReportType = "LeaseExpiry",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                Leases = leaseList,
                TotalRecords = leaseList.Count,
                ExpiredLeases = leaseList.Count
            };
        }

        // Employee Reports
        public async Task<EmployeeReportModel> GetEmployeeReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    e.Id,
                    e.Name,
                    e.EmployeeCode,
                    d.Name as Department,
                    des.Name as Designation,
                    e.HRA,
                    COUNT(l.Id) as ActiveLeases,
                    SUM(l.RentAmount) as TotalRentAmount,
                    e.Status,
                    e.CreatedAt
                FROM Employees e
                LEFT JOIN Departments d ON e.DepartmentId = d.Id
                LEFT JOIN Designations des ON e.DesignationId = des.Id
                LEFT JOIN Leases l ON e.Id = l.EmployeeId AND l.Status = 'Active' AND l.IsActiveRecord = 1
                WHERE e.IsActive = 1
                AND (@FromDate IS NULL OR e.CreatedAt >= @FromDate)
                AND (@ToDate IS NULL OR e.CreatedAt <= @ToDate)
                AND (@Department IS NULL OR d.Name = @Department)
                AND (@Designation IS NULL OR des.Name = @Designation)
                GROUP BY e.Id, e.Name, e.EmployeeCode, d.Name, des.Name, e.HRA, e.Status, e.CreatedAt
                ORDER BY e.Name";

            var parameters = BuildReportParameters(filter);
            var employees = await connection.QueryAsync<EmployeeReportItem>(sql, parameters);
            var employeeList = employees.ToList();

            return new EmployeeReportModel
            {
                ReportTitle = "Employee Report",
                ReportType = "Employee",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                Employees = employeeList,
                TotalRecords = employeeList.Count,
                TotalEmployees = employeeList.Count,
                ActiveEmployees = employeeList.Count(e => e.Status == "Active"),
                InactiveEmployees = employeeList.Count(e => e.Status == "Inactive"),
                TotalHRA = employeeList.Sum(e => e.HRA ?? 0)
            };
        }

        // Vendor Reports
        public async Task<VendorReportModel> GetVendorReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    v.Id,
                    v.VendorName,
                    v.ContactPerson,
                    v.Phone,
                    v.Email,
                    v.Address,
                    COUNT(l.Id) as ActiveLeases,
                    SUM(l.RentAmount) as TotalRentAmount,
                    SUM(l.MaintenancePayment) as TotalMaintenanceAmount,
                    v.Status,
                    v.CreatedAt
                FROM Vendors v
                LEFT JOIN Leases l ON v.Id = l.VendorId AND l.Status = 'Active' AND l.IsActiveRecord = 1
                WHERE v.IsActive = 1
                AND (@FromDate IS NULL OR v.CreatedAt >= @FromDate)
                AND (@ToDate IS NULL OR v.CreatedAt <= @ToDate)
                GROUP BY v.Id, v.VendorName, v.ContactPerson, v.Phone, v.Email, v.Address, v.Status, v.CreatedAt
                ORDER BY v.VendorName";

            var parameters = BuildReportParameters(filter);
            var vendors = await connection.QueryAsync<VendorReportItem>(sql, parameters);
            var vendorList = vendors.ToList();

            return new VendorReportModel
            {
                ReportTitle = "Vendor Report",
                ReportType = "Vendor",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                Vendors = vendorList,
                TotalRecords = vendorList.Count,
                TotalVendors = vendorList.Count,
                ActiveVendors = vendorList.Count(v => v.Status == "Active"),
                TotalRentAmount = vendorList.Sum(v => v.TotalRentAmount),
                TotalMaintenanceAmount = vendorList.Sum(v => v.TotalMaintenanceAmount)
            };
        }

        // Financial Reports
        public async Task<FinancialReportModel> GetFinancialReportAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    FORMAT(l.CreatedAt, 'yyyy-MM') as Month,
                    YEAR(l.CreatedAt) as Year,
                    SUM(l.RentAmount) as RentRevenue,
                    SUM(l.MaintenancePayment) as MaintenanceRevenue,
                    SUM(l.RentAmount + l.MaintenancePayment) as TotalRevenue,
                    0 as Expenses,
                    SUM(l.RentAmount + l.MaintenancePayment) as NetAmount,
                    COUNT(l.Id) as TotalLeases,
                    COUNT(CASE WHEN l.Status = 'Active' THEN 1 END) as ActiveLeases
                FROM Leases l
                WHERE l.IsActiveRecord = 1
                AND (@FromDate IS NULL OR l.CreatedAt >= @FromDate)
                AND (@ToDate IS NULL OR l.CreatedAt <= @ToDate)
                GROUP BY FORMAT(l.CreatedAt, 'yyyy-MM'), YEAR(l.CreatedAt)
                ORDER BY Year DESC, Month DESC";

            var parameters = BuildReportParameters(filter);
            var financialData = await connection.QueryAsync<FinancialReportItem>(sql, parameters);
            var financialList = financialData.ToList();

            return new FinancialReportModel
            {
                ReportTitle = "Financial Report",
                ReportType = "Financial",
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                GeneratedBy = "System",
                FinancialData = financialList,
                TotalRecords = financialList.Count,
                TotalRevenue = financialList.Sum(f => f.TotalRevenue),
                TotalExpenses = financialList.Sum(f => f.Expenses),
                NetAmount = financialList.Sum(f => f.NetAmount)
            };
        }

        // Dashboard Reports
        public async Task<DashboardReportModel> GetDashboardStatisticsAsync()
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    COUNT(*) as TotalLeases,
                    COUNT(CASE WHEN Status = 'Active' THEN 1 END) as ActiveLeases,
                    COUNT(CASE WHEN Status = 'Expired' THEN 1 END) as ExpiredLeases,
                    COUNT(CASE WHEN ApprovalStatus = 1 THEN 1 END) as PendingApprovals,
                    SUM(MonthlyRentPayable) as TotalMonthlyRent,
                    SUM(MaintenancePayment) as TotalMaintenancePayment
                FROM Leases 
                WHERE IsActiveRecord = 1";

            var leaseStats = await connection.QueryFirstOrDefaultAsync(sql);

            var employeeCountSql = "SELECT COUNT(*) FROM Employees WHERE IsActive = 1";
            var vendorCountSql = "SELECT COUNT(*) FROM Vendors WHERE IsActive = 1";

            var employeeCount = await connection.ExecuteScalarAsync<int>(employeeCountSql);
            var vendorCount = await connection.ExecuteScalarAsync<int>(vendorCountSql);

            return new DashboardReportModel
            {
                TotalLeases = leaseStats?.TotalLeases ?? 0,
                ActiveLeases = leaseStats?.ActiveLeases ?? 0,
                ExpiredLeases = leaseStats?.ExpiredLeases ?? 0,
                PendingApprovals = leaseStats?.PendingApprovals ?? 0,
                TotalEmployees = employeeCount,
                TotalVendors = vendorCount,
                TotalMonthlyRent = leaseStats?.TotalMonthlyRent ?? 0,
                TotalMaintenancePayment = leaseStats?.TotalMaintenancePayment ?? 0
            };
        }

        public async Task<List<MonthlyData>> GetMonthlyTrendsAsync(int year)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    FORMAT(CreatedAt, 'MMM') as Month,
                    YEAR(CreatedAt) as Year,
                    COUNT(*) as NewLeases,
                    SUM(RentAmount) as TotalRent,
                    COUNT(CASE WHEN ApprovalStatus = 2 THEN 1 END) as Approvals
                FROM Leases 
                WHERE IsActiveRecord = 1 AND YEAR(CreatedAt) = @Year
                GROUP BY FORMAT(CreatedAt, 'MMM'), YEAR(CreatedAt)
                ORDER BY MIN(CreatedAt)";

            var trends = await connection.QueryAsync<MonthlyData>(sql, new { Year = year });
            return trends.ToList();
        }

        public async Task<List<DepartmentData>> GetDepartmentStatisticsAsync()
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT 
                    d.Name as Department,
                    COUNT(e.Id) as EmployeeCount,
                    COUNT(l.Id) as ActiveLeases,
                    SUM(l.RentAmount) as TotalRentAmount
                FROM Departments d
                LEFT JOIN Employees e ON d.Id = e.DepartmentId AND e.IsActive = 1
                LEFT JOIN Leases l ON e.Id = l.EmployeeId AND l.Status = 'Active' AND l.IsActiveRecord = 1
                WHERE d.IsActive = 1
                GROUP BY d.Id, d.Name
                ORDER BY d.Name";

            var deptStats = await connection.QueryAsync<DepartmentData>(sql);
            return deptStats.ToList();
        }

        public async Task<List<RecentActivity>> GetRecentActivitiesAsync(int count = 10)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT TOP (@Count)
                    Id,
                    'Lease' as ActivityType,
                    'Lease ' + RefNo + ' was ' + 
                    CASE 
                        WHEN ApprovalStatus = 1 THEN 'created and pending approval'
                        WHEN ApprovalStatus = 2 THEN 'approved'
                        WHEN ApprovalStatus = 3 THEN 'rejected'
                        ELSE 'updated'
                    END as Description,
                    COALESCE(MakerUserName, 'System') as UserName,
                    CreatedAt as Timestamp,
                    CASE 
                        WHEN ApprovalStatus = 1 THEN 'Pending'
                        WHEN ApprovalStatus = 2 THEN 'Approved'
                        WHEN ApprovalStatus = 3 THEN 'Rejected'
                        ELSE 'Active'
                    END as Status
                FROM Leases 
                WHERE IsActiveRecord = 1
                ORDER BY CreatedAt DESC";

            var activities = await connection.QueryAsync<RecentActivity>(sql, new { Count = count });
            return activities.ToList();
        }

        // Export Methods
        public async Task<byte[]> ExportToPdfAsync(BaseReportModel report, string templatePath)
        {
            // Implementation for PDF export
            // You can use libraries like iTextSharp, PdfSharp, or DinkToPdf
            throw new NotImplementedException("PDF export not implemented yet");
        }

        public async Task<byte[]> ExportToExcelAsync(BaseReportModel report)
        {
            // Implementation for Excel export
            // You can use libraries like EPPlus, NPOI, or ClosedXML
            throw new NotImplementedException("Excel export not implemented yet");
        }

        public async Task<byte[]> ExportToCsvAsync(BaseReportModel report)
        {
            // Implementation for CSV export
            var csv = new StringBuilder();
            
            // Add headers based on report type
            if (report is LeaseReportModel leaseReport)
            {
                csv.AppendLine("Ref No,Employee Name,Vendor Name,Lease Type,Monthly Rent,Rent Amount,Maintenance Payment,From Date,End Date,Status,Approval Status");
                
                foreach (var lease in leaseReport.Leases)
                {
                    csv.AppendLine($"{lease.RefNo},{lease.EmployeeName},{lease.VendorName},{lease.LeaseTypeName}," +
                                 $"{lease.MonthlyRentPayable},{lease.RentAmount},{lease.MaintenancePayment}," +
                                 $"{lease.FromDate:yyyy-MM-dd},{lease.EndDate:yyyy-MM-dd},{lease.Status},{lease.ApprovalStatus}");
                }
            }
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // Utility Methods
        public async Task<int> GetTotalLeasesCountAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            var sql = "SELECT COUNT(*) FROM Leases WHERE IsActiveRecord = 1";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetTotalEmployeesCountAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            var sql = "SELECT COUNT(*) FROM Employees WHERE IsActive = 1";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetTotalVendorsCountAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            var sql = "SELECT COUNT(*) FROM Vendors WHERE IsActive = 1";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<decimal> GetTotalRentAmountAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            var sql = "SELECT SUM(RentAmount) FROM Leases WHERE IsActiveRecord = 1";
            return await connection.ExecuteScalarAsync<decimal?>(sql) ?? 0;
        }

        public async Task<decimal> GetTotalMaintenanceAmountAsync(ReportFilterModel filter)
        {
            using var connection = CreateConnection();
            var sql = "SELECT SUM(MaintenancePayment) FROM Leases WHERE IsActiveRecord = 1";
            return await connection.ExecuteScalarAsync<decimal?>(sql) ?? 0;
        }

        // Helper Methods
        private string BuildLeaseReportQuery(ReportFilterModel filter)
        {
            var sql = @"
                SELECT 
                    l.Id,
                    l.RefNo,
                    e.Name as EmployeeName,
                    v.VendorName,
                    lt.Name as LeaseTypeName,
                    l.MonthlyRentPayable,
                    l.RentAmount,
                    l.MaintenancePayment,
                    l.FromDate,
                    l.EndDate,
                    l.Status,
                    CASE 
                        WHEN l.ApprovalStatus = 1 THEN 'Pending'
                        WHEN l.ApprovalStatus = 2 THEN 'Approved'
                        WHEN l.ApprovalStatus = 3 THEN 'Rejected'
                        ELSE 'Unknown'
                    END as ApprovalStatus,
                    l.CreatedAt,
                    l.MakerUserName,
                    l.CheckerUserName
                FROM Leases l
                LEFT JOIN Employees e ON l.EmployeeId = e.Id
                LEFT JOIN Vendors v ON l.VendorId = v.Id
                LEFT JOIN LeaseTypes lt ON l.LeaseTypeId = lt.Id
                WHERE l.IsActiveRecord = 1";

            if (filter.FromDate.HasValue)
                sql += " AND l.CreatedAt >= @FromDate";
            if (filter.ToDate.HasValue)
                sql += " AND l.CreatedAt <= @ToDate";
            if (filter.EmployeeId.HasValue)
                sql += " AND l.EmployeeId = @EmployeeId";
            if (filter.VendorId.HasValue)
                sql += " AND l.VendorId = @VendorId";
            if (filter.LeaseTypeId.HasValue)
                sql += " AND l.LeaseTypeId = @LeaseTypeId";
            if (!string.IsNullOrEmpty(filter.Status))
                sql += " AND l.Status = @Status";
            if (!string.IsNullOrEmpty(filter.ApprovalStatus))
                sql += " AND l.ApprovalStatus = @ApprovalStatus";

            sql += " ORDER BY l.CreatedAt DESC";
            return sql;
        }

        private object BuildReportParameters(ReportFilterModel filter)
        {
            return new
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                EmployeeId = filter.EmployeeId,
                VendorId = filter.VendorId,
                LeaseTypeId = filter.LeaseTypeId,
                Status = filter.Status,
                ApprovalStatus = filter.ApprovalStatus,
                Department = filter.Department,
                Designation = filter.Designation,
                SearchTerm = filter.SearchTerm
            };
        }

        // Implement remaining methods with similar patterns...
        public async Task<PaymentReportModel> GetPaymentReportAsync(ReportFilterModel filter)
        {
            // Implementation for payment reports
            throw new NotImplementedException();
        }

        public async Task<PaymentReportModel> GetPaymentSummaryReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentReportModel> GetOverduePaymentReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentReportModel> GetPaymentCollectionReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<EmployeeReportModel> GetEmployeeLeaseReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<EmployeeReportModel> GetEmployeeHRAReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<EmployeeReportModel> GetDepartmentWiseReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<VendorReportModel> GetVendorLeaseReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<VendorReportModel> GetVendorPaymentReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<FinancialReportModel> GetMonthlyFinancialReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<FinancialReportModel> GetYearlyFinancialReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<FinancialReportModel> GetRevenueReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<ApprovalReportModel> GetApprovalReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<ApprovalReportModel> GetApprovalSummaryReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<ApprovalReportModel> GetApprovalPendingReportAsync(ReportFilterModel filter)
        {
            throw new NotImplementedException();
        }

        public async Task<DashboardReportModel> GetDashboardStatisticsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }

        // TDS Report Implementations
        public async Task<TDSReportModel> GetTDSReportAsync(TDSReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            // For export, let's use a simpler query without pagination to get all data
            var isExport = filter.Page == 0; // If page is 0, treat as export
            
            if (isExport)
            {
                // Simple query for export without complex filters
                var exportSql = @"
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY mr.PaymentDate DESC) as SlNo,
                        'CN/' + RIGHT('000000' + CAST(mr.Id AS VARCHAR(6)), 6) + '/25-26' as VoucherNumber,
                        ISNULL(e.Code, '') as EmployeeCode,
                        ISNULL(e.Name, '') as EmployeeName,
                        ISNULL(v.VendorCode, '') as VendorCode,
                        ISNULL(v.VendorName, '') as VendorName,
                        ISNULL(v.PanNumber, '') as PAN,
                        mr.PaymentDate,
                        ISNULL(mr.MonthlyLeaseAmount, 0) as TDSDeductedOn,
                        ISNULL(mr.TDSRate, 0) as TDSRate,
                        ISNULL(mr.TDSAmount, 0) as TDSAmount,
                        ISNULL(mr.MonthlyLeaseAmount, 0) as GrossValue,
                        '' as ProjectName,
                        '70800 : Rent Paid (Rent Free Accommodation)' as CoAName,
                        'Rent Payable for the Month of ' + ISNULL(DATENAME(MONTH, mr.PaymentMonth), '') + ' ' + ISNULL(CAST(YEAR(mr.PaymentMonth) AS VARCHAR(4)), '') + 
                        ' on behalf of ' + ISNULL(e.Code, '') + ': ' + ISNULL(e.Name, '') + ' (@' + ISNULL(CAST(mr.MonthlyLeaseAmount AS VARCHAR(20)), '0') + '/-, ' + ISNULL(CAST(mr.TDSRate AS VARCHAR(10)), '0') + '%TDS deducted)' as Narration,
                        '' as ExemptionCertificateNo,
                        NULL as ExemptionFromDate,
                        NULL as ExemptionToDate,
                        'MonthlyRent' as PaymentType,
                        ISNULL(mr.PaymentStatus, '') as PaymentStatus,
                        ISNULL(l.RefNo, '') as LeaseRefNo,
                        ISNULL(DATENAME(MONTH, mr.PaymentMonth), '') as Month,
                        ISNULL(CAST(YEAR(mr.PaymentMonth) AS VARCHAR(4)), '') as Year,
                        ISNULL(CAST(YEAR(mr.PaymentMonth) AS VARCHAR(4)), '') + '-' + ISNULL(RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2), '') as FinancialYear,
                        ISNULL(t.Name, '') as TDSApplicableName,
                        '' as TransactionReference,
                        '' as Remarks,
                        ISNULL(mr.CreatedDate, GETDATE()) as CreatedDate,
                        '' as CreatedBy
                    FROM MonthlyRentPayments mr
                    LEFT JOIN Employees e ON mr.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON mr.VendorId = v.Id
                    LEFT JOIN Leases l ON mr.LeaseId = l.Id
                    LEFT JOIN TDSApplicable t ON mr.TDSApplicableId = t.Id
                    WHERE mr.TDSAmount > 0
                    ORDER BY mr.PaymentDate DESC";

                var tdsItems = await connection.QueryAsync<TDSReportItem>(exportSql);
                var tdsList = tdsItems.ToList();

                // Add serial numbers
                for (int i = 0; i < tdsList.Count; i++)
                {
                    tdsList[i].SlNo = i + 1;
                }

                var summary = await CalculateTDSSummaryAsync(connection, filter);

                return new TDSReportModel
                {
                    ReportTitle = "TDS Report - Lease Payable",
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = "System",
                    Filter = filter,
                    TDSItems = tdsList,
                    Summary = summary,
                    TotalRecords = tdsList.Count,
                    ReportType = "TDS"
                };
            }
            else
            {
                // Get total count first
                var countSql = BuildTDSReportCountQuery(filter);
                var countParameters = BuildTDSReportParameters(filter);
                var totalRecords = await connection.QueryFirstOrDefaultAsync<int>(countSql, countParameters);
                
                // Get paginated data
                var sql = BuildTDSReportQuery(filter);
                var parameters = BuildTDSReportParameters(filter);
                
                // Add pagination parameters
                parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);
                parameters.Add("@PageSize", filter.PageSize);

                var tdsItems = await connection.QueryAsync<TDSReportItem>(sql, parameters);
                var tdsList = tdsItems.ToList();

                // Add serial numbers based on page
                var startNumber = (filter.Page - 1) * filter.PageSize + 1;
                for (int i = 0; i < tdsList.Count; i++)
                {
                    tdsList[i].SlNo = startNumber + i;
                }

                var summary = await CalculateTDSSummaryAsync(connection, filter);

                return new TDSReportModel
                {
                    ReportTitle = "TDS Report - Lease Payable",
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = "System",
                    Filter = filter,
                    TDSItems = tdsList,
                    Summary = summary,
                    TotalRecords = totalRecords,
                    ReportType = "TDS"
                };
            }
        }

        public async Task<TDSReportModel> GetTDSMonthlyReportAsync(TDSReportFilterModel filter)
        {
            // Similar to GetTDSReportAsync but with monthly grouping
            return await GetTDSReportAsync(filter);
        }

        public async Task<TDSReportModel> GetTDSVendorReportAsync(TDSReportFilterModel filter)
        {
            // Similar to GetTDSReportAsync but with vendor grouping
            return await GetTDSReportAsync(filter);
        }

        public async Task<TDSReportModel> GetTDSEmployeeReportAsync(TDSReportFilterModel filter)
        {
            // Similar to GetTDSReportAsync but with employee grouping
            return await GetTDSReportAsync(filter);
        }

        public async Task<TDSChartData> GetTDSChartDataAsync(TDSReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var chartData = new TDSChartData();

            // Get monthly trends
            var monthlyTrendsSql = @"
                SELECT 
                    DATENAME(MONTH, PaymentDate) as Month,
                    YEAR(PaymentDate) as Year,
                    SUM(TDSAmount) as TotalTDS,
                    SUM(TDSDeductedOn) as TotalGross,
                    COUNT(*) as PaymentCount
                FROM (
                    SELECT PaymentDate, TDSAmount, TDSDeductedOn
                    FROM MonthlyRentPayments 
                    WHERE TDSAmount > 0
                    UNION ALL
                    SELECT PaymentDate, TDSAmount, BrokerageAmount as TDSDeductedOn
                    FROM BrokeragePayment 
                    WHERE TDSAmount > 0
                ) t
                WHERE (@FromDate IS NULL OR PaymentDate >= @FromDate)
                AND (@ToDate IS NULL OR PaymentDate <= @ToDate)
                GROUP BY DATENAME(MONTH, PaymentDate), YEAR(PaymentDate)
                ORDER BY YEAR(PaymentDate), MONTH(PaymentDate)";

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);

            chartData.MonthlyTrends = (await connection.QueryAsync<TDSMonthlyTrend>(monthlyTrendsSql, parameters)).ToList();

            return chartData;
        }

        public async Task<TDSPeggingModel> GetTDSPeggingAsync(int paymentId, string paymentType)
        {
            using var connection = CreateConnection();
            
            var peggingModel = new TDSPeggingModel
            {
                PaymentId = paymentId,
                PaymentType = paymentType
            };

            if (paymentType == "MonthlyRent")
            {
                var sql = @"
                    SELECT 
                        mr.Id as PaymentId,
                        mr.PaymentDate,
                        mr.MonthlyLeaseAmount as Amount,
                        mr.PaymentStatus as Status,
                        l.RefNo as LeaseRefNo,
                        e.Name as EmployeeName,
                        v.VendorName,
                        mr.MonthlyLeaseAmount as AllocatedAmount,
                        mr.TDSAmount,
                        DATENAME(MONTH, mr.PaymentMonth) as Month,
                        YEAR(mr.PaymentMonth) as Year
                    FROM MonthlyRentPayments mr
                    INNER JOIN Leases l ON mr.LeaseId = l.Id
                    INNER JOIN Employees e ON mr.EmployeeId = e.Id
                    INNER JOIN Vendors v ON mr.VendorId = v.Id
                    WHERE mr.Id = @PaymentId";

                var result = await connection.QueryFirstOrDefaultAsync<TDSPeggingDetail>(sql, new { PaymentId = paymentId });
                if (result != null)
                {
                    peggingModel.VoucherNumber = $"CN/{paymentId:D6}/25-26";
                    peggingModel.PaymentDate = result.PaymentDate;
                    peggingModel.Amount = result.Amount;
                    peggingModel.Status = result.Status;
                    peggingModel.PeggingDetails.Add(result);
                }
            }

            return peggingModel;
        }

        // TDS Brokerage Report Implementations
        public async Task<TDSBrokerageReportModel> GetTDSBrokerageReportAsync(TDSBrokerageReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            // For export, let's use a simpler query without pagination to get all data
            var isExport = filter.Page == 0; // If page is 0, treat as export
            
            if (isExport)
            {
                // Simple query for export without complex filters
                var exportSql = @"
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY bp.PaymentDate DESC) as SlNo,
                        'CN/' + RIGHT('000000' + CAST(bp.Id AS VARCHAR(6)), 6) + '/25-26' as VoucherNumber,
                        ISNULL(e.Code, '') as EmployeeCode,
                        ISNULL(e.Name, '') as EmployeeName,
                        ISNULL(v.VendorCode, '') as VendorCode,
                        ISNULL(v.VendorName, '') as VendorName,
                        ISNULL(v.PanNumber, '') as PAN,
                        bp.PaymentDate,
                        ISNULL(bp.BrokerageAmount, 0) as TDSDeductedOn,
                        ISNULL(bp.TDSRate, 0) as TDSRate,
                        ISNULL(bp.TDSAmount, 0) as TDSAmount,
                        ISNULL(bp.BrokerageAmount, 0) as GrossValue,
                        '' as ProjectName,
                        '70800 : Rent Paid (Rent Free Accommodation)' as CoAName,
                        'Brokerage Payment for the Month of ' + ISNULL(DATENAME(MONTH, bp.PaymentMonth), '') + ' ' + ISNULL(CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)), '') + 
                        ' on behalf of ' + ISNULL(e.Code, '') + ': ' + ISNULL(e.Name, '') + ' (@' + ISNULL(CAST(bp.BrokerageAmount AS VARCHAR(20)), '0') + '/-, ' + ISNULL(CAST(bp.TDSRate AS VARCHAR(10)), '0') + '%TDS deducted)' as Narration,
                        '' as ExemptionCertificateNo,
                        NULL as ExemptionFromDate,
                        NULL as ExemptionToDate,
                        'Brokerage' as PaymentType,
                        ISNULL(bp.PaymentStatus, '') as PaymentStatus,
                        ISNULL(l.RefNo, '') as LeaseRefNo,
                        ISNULL(DATENAME(MONTH, bp.PaymentMonth), '') as Month,
                        ISNULL(CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)), '') as Year,
                        ISNULL(CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)), '') + '-' + ISNULL(RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2), '') as FinancialYear,
                        ISNULL(t.Name, '') as TDSApplicableName,
                        '' as TransactionReference,
                        '' as Remarks,
                        ISNULL(bp.CreatedDate, GETDATE()) as CreatedDate,
                        '' as CreatedBy,
                        ISNULL(bp.NetPayableAmount, 0) as NetPayableAmount,
                        ISNULL(bp.DSCApprovalStatus, '') as DSCApprovalStatus
                    FROM BrokeragePayment bp
                    LEFT JOIN Employees e ON bp.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON bp.VendorId = v.Id
                    LEFT JOIN Leases l ON bp.LeaseId = l.Id
                    LEFT JOIN TDSApplicable t ON bp.TDSApplicableId = t.Id
                    WHERE bp.TDSAmount > 0
                    ORDER BY bp.PaymentDate DESC";

                var tdsItems = await connection.QueryAsync<TDSBrokerageReportItem>(exportSql);
                var tdsList = tdsItems.ToList();

                // Add serial numbers
                for (int i = 0; i < tdsList.Count; i++)
                {
                    tdsList[i].SlNo = i + 1;
                }

                var summary = await CalculateTDSBrokerageSummaryAsync(connection, filter);

                return new TDSBrokerageReportModel
                {
                    ReportTitle = "TDS Brokerage Report - Lease Brokerage",
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = "System",
                    Filter = filter,
                    TDSItems = tdsList,
                    Summary = summary,
                    TotalRecords = tdsList.Count,
                    ReportType = "TDSBrokerage"
                };
            }
            else
            {
                // Get total count first
                var countSql = BuildTDSBrokerageReportCountQuery(filter);
                var countParameters = BuildTDSBrokerageReportParameters(filter);
                var totalRecords = await connection.QueryFirstOrDefaultAsync<int>(countSql, countParameters);
                
                // Get paginated data
                var sql = BuildTDSBrokerageReportQuery(filter);
                var parameters = BuildTDSBrokerageReportParameters(filter);
                
                // Add pagination parameters
                parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);
                parameters.Add("@PageSize", filter.PageSize);

                var tdsItems = await connection.QueryAsync<TDSBrokerageReportItem>(sql, parameters);
                var tdsList = tdsItems.ToList();

                // Add serial numbers based on page
                var startNumber = (filter.Page - 1) * filter.PageSize + 1;
                for (int i = 0; i < tdsList.Count; i++)
                {
                    tdsList[i].SlNo = startNumber + i;
                }

                var summary = await CalculateTDSBrokerageSummaryAsync(connection, filter);

                return new TDSBrokerageReportModel
                {
                    ReportTitle = "TDS Brokerage Report - Lease Brokerage",
                    GeneratedDate = DateTime.Now,
                    GeneratedBy = "System",
                    Filter = filter,
                    TDSItems = tdsList,
                    Summary = summary,
                    TotalRecords = totalRecords,
                    ReportType = "TDSBrokerage"
                };
            }
        }

        public async Task<TDSBrokerageReportModel> GetTDSBrokerageMonthlyReportAsync(TDSBrokerageReportFilterModel filter)
        {
            // Similar to GetTDSBrokerageReportAsync but with monthly grouping
            return await GetTDSBrokerageReportAsync(filter);
        }

        public async Task<TDSBrokerageReportModel> GetTDSBrokerageVendorReportAsync(TDSBrokerageReportFilterModel filter)
        {
            // Similar to GetTDSBrokerageReportAsync but with vendor grouping
            return await GetTDSBrokerageReportAsync(filter);
        }

        public async Task<TDSBrokerageReportModel> GetTDSBrokerageEmployeeReportAsync(TDSBrokerageReportFilterModel filter)
        {
            // Similar to GetTDSBrokerageReportAsync but with employee grouping
            return await GetTDSBrokerageReportAsync(filter);
        }

        public async Task<TDSBrokerageChartData> GetTDSBrokerageChartDataAsync(TDSBrokerageReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var chartData = new TDSBrokerageChartData();

            // Get monthly trends
            var monthlyTrendsSql = @"
                SELECT 
                    DATENAME(MONTH, PaymentDate) as Month,
                    YEAR(PaymentDate) as Year,
                    SUM(TDSAmount) as TotalTDS,
                    SUM(BrokerageAmount) as TotalGross,
                    COUNT(*) as PaymentCount
                FROM BrokeragePayment 
                WHERE TDSAmount > 0
                AND (@FromDate IS NULL OR PaymentDate >= @FromDate)
                AND (@ToDate IS NULL OR PaymentDate <= @ToDate)
                GROUP BY DATENAME(MONTH, PaymentDate), YEAR(PaymentDate)
                ORDER BY YEAR(PaymentDate), MONTH(PaymentDate)";

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);

            chartData.MonthlyTrends = (await connection.QueryAsync<TDSBrokerageMonthlyTrend>(monthlyTrendsSql, parameters)).ToList();

            return chartData;
        }

        public async Task<TDSBrokeragePeggingModel> GetTDSBrokeragePeggingAsync(int paymentId, string paymentType)
        {
            using var connection = CreateConnection();
            
            var peggingModel = new TDSBrokeragePeggingModel
            {
                PaymentId = paymentId,
                PaymentType = paymentType
            };

            if (paymentType == "Brokerage")
            {
                var sql = @"
                    SELECT 
                        bp.Id as PaymentId,
                        bp.PaymentDate,
                        bp.BrokerageAmount as Amount,
                        bp.PaymentStatus as Status,
                        l.RefNo as LeaseRefNo,
                        e.Name as EmployeeName,
                        v.VendorName,
                        bp.BrokerageAmount as AllocatedAmount,
                        bp.TDSAmount,
                        DATENAME(MONTH, bp.PaymentMonth) as Month,
                        YEAR(bp.PaymentMonth) as Year,
                        bp.NetPayableAmount
                    FROM BrokeragePayment bp
                    INNER JOIN Leases l ON bp.LeaseId = l.Id
                    INNER JOIN Employees e ON bp.EmployeeId = e.Id
                    INNER JOIN Vendors v ON bp.VendorId = v.Id
                    WHERE bp.Id = @PaymentId";

                var result = await connection.QueryFirstOrDefaultAsync<TDSBrokeragePeggingDetail>(sql, new { PaymentId = paymentId });
                if (result != null)
                {
                    peggingModel.VoucherNumber = $"CN/{paymentId:D6}/25-26";
                    peggingModel.PaymentDate = result.PaymentDate;
                    peggingModel.Amount = result.Amount;
                    peggingModel.Status = result.Status;
                    peggingModel.PeggingDetails.Add(result);
                }
            }

            return peggingModel;
        }

        public async Task<byte[]> ExportTDSReportToPdfAsync(TDSReportModel report, TDSReportExportOptions options)
        {
            try
            {
                var html = new StringBuilder();
                
                // HTML header
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset='utf-8'>");
                html.AppendLine("<title>TDS Report - Lease Payable</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; font-size: 12px; }");
                html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                html.AppendLine("th, td { border: 1px solid #ddd; padding: 6px; text-align: left; font-size: 11px; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".header { text-align: center; margin-bottom: 20px; }");
                html.AppendLine(".summary { margin-bottom: 20px; }");
                html.AppendLine(".filters { margin-bottom: 20px; }");
                html.AppendLine(".amount { text-align: right; }");
                html.AppendLine("@media print {");
                html.AppendLine("  body { margin: 0; }");
                html.AppendLine("  table { page-break-inside: auto; }");
                html.AppendLine("  tr { page-break-inside: avoid; page-break-after: auto; }");
                html.AppendLine("  thead { display: table-header-group; }");
                html.AppendLine("  tfoot { display: table-footer-group; }");
                html.AppendLine("}");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div style='text-align: right; margin-bottom: 20px;'>");
                html.AppendLine("<button onclick='window.print()' style='padding: 10px 20px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;'>Print as PDF</button>");
                html.AppendLine("<p style='font-size: 10px; color: #666; margin-top: 10px;'>Click 'Print as PDF' button, then in the print dialog select 'Save as PDF' as the destination.</p>");
                html.AppendLine("</div>");
                
                // Report header
                html.AppendLine("<div class='header'>");
                html.AppendLine($"<h1>{options.ReportTitle}</h1>");
                html.AppendLine($"<p>Generated Date: {report.GeneratedDate:dd/MM/yyyy HH:mm:ss}</p>");
                html.AppendLine($"<p>Generated By: {report.GeneratedBy}</p>");
                html.AppendLine($"<p>Total Records: {report.TotalRecords}</p>");
                html.AppendLine("</div>");
                
                // Summary section
                if (options.IncludeSummary)
                {
                    html.AppendLine("<div class='summary'>");
                    html.AppendLine("<h2>Summary</h2>");
                    html.AppendLine("<table>");
                    html.AppendLine("<tr><td>Total Payments</td><td>" + report.Summary.TotalPayments + "</td></tr>");
                    html.AppendLine("<tr><td>Total Gross Amount</td><td class='amount'>₹" + report.Summary.TotalGrossAmount.ToString("N2") + "</td></tr>");
                    html.AppendLine("<tr><td>Total TDS Amount</td><td class='amount'>₹" + report.Summary.TotalTDSAmount.ToString("N2") + "</td></tr>");
                    html.AppendLine("<tr><td>Total Net Amount</td><td class='amount'>₹" + report.Summary.TotalNetAmount.ToString("N2") + "</td></tr>");
                    html.AppendLine("<tr><td>Average TDS Rate</td><td class='amount'>" + report.Summary.AverageTDSRate.ToString("N2") + "%</td></tr>");
                    html.AppendLine("<tr><td>Unique Employees</td><td>" + report.Summary.UniqueEmployees + "</td></tr>");
                    html.AppendLine("<tr><td>Unique Vendors</td><td>" + report.Summary.UniqueVendors + "</td></tr>");
                    html.AppendLine("<tr><td>Unique PANs</td><td>" + report.Summary.UniquePANs + "</td></tr>");
                    html.AppendLine("</table>");
                    html.AppendLine("</div>");
                }
                
                // Filters section
                if (options.IncludeFilters)
                {
                    html.AppendLine("<div class='filters'>");
                    html.AppendLine("<h2>Filters Applied</h2>");
                    html.AppendLine("<table>");
                    html.AppendLine("<tr><td>From Date</td><td>" + (report.Filter.FromDate?.ToString("dd/MM/yyyy") ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>To Date</td><td>" + (report.Filter.ToDate?.ToString("dd/MM/yyyy") ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>Employee Code</td><td>" + (report.Filter.EmployeeCode ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>Employee Name</td><td>" + (report.Filter.EmployeeName ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>Vendor Code</td><td>" + (report.Filter.VendorCode ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>Vendor Name</td><td>" + (report.Filter.VendorName ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>PAN</td><td>" + (report.Filter.PAN ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>Financial Year</td><td>" + (report.Filter.FinancialYear ?? "All") + "</td></tr>");
                    html.AppendLine("<tr><td>Month</td><td>" + (report.Filter.Month ?? "All") + "</td></tr>");
                    html.AppendLine("</table>");
                    html.AppendLine("</div>");
                }
                
                // Data table
                html.AppendLine("<h2>TDS Report Data</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Sl No</th>");
                html.AppendLine("<th>Voucher No.</th>");
                html.AppendLine("<th>Code</th>");
                html.AppendLine("<th>Name</th>");
                html.AppendLine("<th>PAN</th>");
                html.AppendLine("<th>Date</th>");
                html.AppendLine("<th>TDS Deducted on</th>");
                html.AppendLine("<th>TDS %</th>");
                html.AppendLine("<th>TDS Amount</th>");
                html.AppendLine("<th>Gross CN Value</th>");
                html.AppendLine("<th>Project Name</th>");
                html.AppendLine("<th>CoA Name</th>");
                html.AppendLine("<th>Narration</th>");
                html.AppendLine("<th>Exemption Certificate No.</th>");
                html.AppendLine("<th>Exemption From Dt.</th>");
                html.AppendLine("<th>Exemption To Dt.</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");
                
                foreach (var item in report.TDSItems)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine("<td>" + item.SlNo + "</td>");
                    html.AppendLine("<td>" + item.VoucherNumber + "</td>");
                    html.AppendLine("<td>" + item.EmployeeCode + "</td>");
                    html.AppendLine("<td>" + item.EmployeeName + "</td>");
                    html.AppendLine("<td>" + item.PAN + "</td>");
                    html.AppendLine("<td>" + item.PaymentDate.ToString("dd/MM/yyyy") + "</td>");
                    html.AppendLine("<td class='amount'>₹" + item.TDSDeductedOn.ToString("N2") + "</td>");
                    html.AppendLine("<td class='amount'>" + item.TDSRate.ToString("N2") + "%</td>");
                    html.AppendLine("<td class='amount'>₹" + item.TDSAmount.ToString("N2") + "</td>");
                    html.AppendLine("<td class='amount'>₹" + item.GrossValue.ToString("N2") + "</td>");
                    html.AppendLine("<td>" + item.ProjectName + "</td>");
                    html.AppendLine("<td>" + item.CoAName + "</td>");
                    html.AppendLine("<td>" + item.Narration + "</td>");
                    html.AppendLine("<td>" + item.ExemptionCertificateNo + "</td>");
                    html.AppendLine("<td>" + (item.ExemptionFromDate?.ToString("dd/MM/yyyy") ?? "") + "</td>");
                    html.AppendLine("<td>" + (item.ExemptionToDate?.ToString("dd/MM/yyyy") ?? "") + "</td>");
                    html.AppendLine("</tr>");
                }
                
                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");
                
                // Return HTML content that can be converted to PDF by the browser
                return System.Text.Encoding.UTF8.GetBytes(html.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ExportTDSReportToExcelAsync(TDSReportModel report, TDSReportExportOptions options)
        {
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("TDS Report");
                
                int currentRow = 1;
                
                // Add header
                worksheet.Cell(currentRow, 1).Value = options.ReportTitle;
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
                worksheet.Range(currentRow, 1, currentRow, 16).Merge();
                currentRow++;
                
                worksheet.Cell(currentRow, 1).Value = $"Generated Date: {report.GeneratedDate:dd/MM/yyyy HH:mm:ss}";
                worksheet.Cell(currentRow, 2).Value = $"Generated By: {report.GeneratedBy}";
                worksheet.Cell(currentRow, 3).Value = $"Total Records: {report.TotalRecords}";
                currentRow += 2;
                
                // Add summary
                if (options.IncludeSummary)
                {
                    worksheet.Cell(currentRow, 1).Value = "SUMMARY";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                    currentRow++;
                    
                    var summaryData = new[]
                    {
                        new { Field = "Total Payments", Value = report.Summary.TotalPayments.ToString() },
                        new { Field = "Total Gross Amount", Value = $"₹{report.Summary.TotalGrossAmount:N2}" },
                        new { Field = "Total TDS Amount", Value = $"₹{report.Summary.TotalTDSAmount:N2}" },
                        new { Field = "Total Net Amount", Value = $"₹{report.Summary.TotalNetAmount:N2}" },
                        new { Field = "Average TDS Rate", Value = $"{report.Summary.AverageTDSRate:N2}%" },
                        new { Field = "Unique Employees", Value = report.Summary.UniqueEmployees.ToString() },
                        new { Field = "Unique Vendors", Value = report.Summary.UniqueVendors.ToString() },
                        new { Field = "Unique PANs", Value = report.Summary.UniquePANs.ToString() }
                    };
                    
                    foreach (var item in summaryData)
                    {
                        worksheet.Cell(currentRow, 1).Value = item.Field;
                        worksheet.Cell(currentRow, 2).Value = item.Value;
                        currentRow++;
                    }
                    currentRow++;
                }
                
                // Add filters
                if (options.IncludeFilters)
                {
                    worksheet.Cell(currentRow, 1).Value = "FILTERS APPLIED";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                    currentRow++;
                    
                    var filterData = new[]
                    {
                        new { Filter = "From Date", Value = report.Filter.FromDate?.ToString("dd/MM/yyyy") ?? "All" },
                        new { Filter = "To Date", Value = report.Filter.ToDate?.ToString("dd/MM/yyyy") ?? "All" },
                        new { Filter = "Employee Code", Value = report.Filter.EmployeeCode ?? "All" },
                        new { Filter = "Employee Name", Value = report.Filter.EmployeeName ?? "All" },
                        new { Filter = "Vendor Code", Value = report.Filter.VendorCode ?? "All" },
                        new { Filter = "Vendor Name", Value = report.Filter.VendorName ?? "All" },
                        new { Filter = "PAN", Value = report.Filter.PAN ?? "All" },
                        new { Filter = "Financial Year", Value = report.Filter.FinancialYear ?? "All" },
                        new { Filter = "Month", Value = report.Filter.Month ?? "All" }
                    };
                    
                    foreach (var item in filterData)
                    {
                        worksheet.Cell(currentRow, 1).Value = item.Filter;
                        worksheet.Cell(currentRow, 2).Value = item.Value;
                        currentRow++;
                    }
                    currentRow++;
                }
                
                // Add data headers
                var headers = new[]
                {
                    "Sl No", "Voucher No.", "Code", "Name", "PAN", "Date", 
                    "TDS Deducted on", "TDS %", "TDS Amount", "Gross CN Value", 
                    "Project Name", "CoA Name", "Narration", "Exemption Certificate No.", 
                    "Exemption From Dt.", "Exemption To Dt."
                };
                
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = headers[i];
                    worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                }
                currentRow++;
                
                // Add data rows
                foreach (var item in report.TDSItems)
                {
                    worksheet.Cell(currentRow, 1).Value = item.SlNo;
                    worksheet.Cell(currentRow, 2).Value = item.VoucherNumber;
                    worksheet.Cell(currentRow, 3).Value = item.EmployeeCode;
                    worksheet.Cell(currentRow, 4).Value = item.EmployeeName;
                    worksheet.Cell(currentRow, 5).Value = item.PAN;
                    worksheet.Cell(currentRow, 6).Value = item.PaymentDate;
                    worksheet.Cell(currentRow, 6).Style.DateFormat.Format = "dd/mm/yyyy";
                    worksheet.Cell(currentRow, 7).Value = item.TDSDeductedOn;
                    worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 8).Value = item.TDSRate;
                    worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(currentRow, 9).Value = item.TDSAmount;
                    worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 10).Value = item.GrossValue;
                    worksheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 11).Value = item.ProjectName;
                    worksheet.Cell(currentRow, 12).Value = item.CoAName;
                    worksheet.Cell(currentRow, 13).Value = item.Narration;
                    worksheet.Cell(currentRow, 14).Value = item.ExemptionCertificateNo;
                    worksheet.Cell(currentRow, 15).Value = item.ExemptionFromDate;
                    if (item.ExemptionFromDate.HasValue)
                        worksheet.Cell(currentRow, 15).Style.DateFormat.Format = "dd/mm/yyyy";
                    worksheet.Cell(currentRow, 16).Value = item.ExemptionToDate;
                    if (item.ExemptionToDate.HasValue)
                        worksheet.Cell(currentRow, 16).Style.DateFormat.Format = "dd/mm/yyyy";
                    currentRow++;
                }
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating Excel: {ex.Message}", ex);
            }
        }

        public async Task<string> ExportTDSReportToCsvAsync(TDSReportModel report, TDSReportExportOptions options)
        {
            var csv = new StringBuilder();
            
            // Add header
            csv.AppendLine("TDS Report - Lease Payable");
            csv.AppendLine($"Generated Date: {report.GeneratedDate:dd/MM/yyyy HH:mm:ss}");
            csv.AppendLine($"Generated By: {report.GeneratedBy}");
            csv.AppendLine($"Total Records: {report.TotalRecords}");
            csv.AppendLine();
            
            // Add summary
            if (options.IncludeSummary)
            {
                csv.AppendLine("SUMMARY");
                csv.AppendLine($"Total Payments,{report.Summary.TotalPayments}");
                csv.AppendLine($"Total Gross Amount,{report.Summary.TotalGrossAmount:N2}");
                csv.AppendLine($"Total TDS Amount,{report.Summary.TotalTDSAmount:N2}");
                csv.AppendLine($"Total Net Amount,{report.Summary.TotalNetAmount:N2}");
                csv.AppendLine($"Average TDS Rate,{report.Summary.AverageTDSRate:N2}%");
                csv.AppendLine($"Unique Employees,{report.Summary.UniqueEmployees}");
                csv.AppendLine($"Unique Vendors,{report.Summary.UniqueVendors}");
                csv.AppendLine($"Unique PANs,{report.Summary.UniquePANs}");
                csv.AppendLine();
            }
            
            // Add filters
            if (options.IncludeFilters)
            {
                csv.AppendLine("FILTERS APPLIED");
                csv.AppendLine($"From Date,{report.Filter.FromDate?.ToString("dd/MM/yyyy") ?? "All"}");
                csv.AppendLine($"To Date,{report.Filter.ToDate?.ToString("dd/MM/yyyy") ?? "All"}");
                csv.AppendLine($"Employee Code,{report.Filter.EmployeeCode ?? "All"}");
                csv.AppendLine($"Employee Name,{report.Filter.EmployeeName ?? "All"}");
                csv.AppendLine($"Vendor Code,{report.Filter.VendorCode ?? "All"}");
                csv.AppendLine($"Vendor Name,{report.Filter.VendorName ?? "All"}");
                csv.AppendLine($"PAN,{report.Filter.PAN ?? "All"}");
                csv.AppendLine($"Financial Year,{report.Filter.FinancialYear ?? "All"}");
                csv.AppendLine($"Month,{report.Filter.Month ?? "All"}");
                csv.AppendLine();
            }
            
            // Add data headers
            csv.AppendLine("Sl No,Voucher No.,Code,Name,PAN,Date,TDS Deducted on,TDS %,TDS Amount,Gross CN Value,Project Name,CoA Name,Narration,Exemption Certificate No.,Exemption From Dt.,Exemption To Dt.");
            
            // Add data rows
            foreach (var item in report.TDSItems)
            {
                csv.AppendLine($"{item.SlNo}," +
                              $"\"{item.VoucherNumber}\"," +
                              $"\"{item.EmployeeCode}\"," +
                              $"\"{item.EmployeeName}\"," +
                              $"\"{item.PAN}\"," +
                              $"\"{item.PaymentDate:dd/MM/yyyy}\"," +
                              $"{item.TDSDeductedOn:N2}," +
                              $"{item.TDSRate:N2}," +
                              $"{item.TDSAmount:N2}," +
                              $"{item.GrossValue:N2}," +
                              $"\"{item.ProjectName}\"," +
                              $"\"{item.CoAName}\"," +
                              $"\"{item.Narration}\"," +
                              $"\"{item.ExemptionCertificateNo}\"," +
                              $"\"{(item.ExemptionFromDate.HasValue ? item.ExemptionFromDate.Value.ToString("dd/MM/yyyy") : string.Empty)}\"," +
                              $"\"{(item.ExemptionToDate.HasValue ? item.ExemptionToDate.Value.ToString("dd/MM/yyyy") : string.Empty)}\"");
            }
            
            return csv.ToString();
        }

        private string BuildTDSReportCountQuery(TDSReportFilterModel filter)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM (
                    SELECT 
                        mr.Id,
                        e.Code,
                        e.Name,
                        v.VendorCode,
                        v.VendorName,
                        v.PanNumber,
                        mr.PaymentDate,
                        mr.MonthlyLeaseAmount,
                        mr.TDSRate,
                        mr.TDSAmount,
                        mr.PaymentMonth
                    FROM MonthlyRentPayments mr
                    INNER JOIN Employees e ON mr.EmployeeId = e.Id
                    INNER JOIN Vendors v ON mr.VendorId = v.Id
                    INNER JOIN Leases l ON mr.LeaseId = l.Id
                    LEFT JOIN TDSApplicable t ON mr.TDSApplicableId = t.Id
                    WHERE 1=1";

            // Add filter conditions
            if (filter.FromDate.HasValue)
                sql += " AND mr.PaymentDate >= @FromDate";
            if (filter.ToDate.HasValue)
                sql += " AND mr.PaymentDate <= @ToDate";
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                sql += " AND e.Code LIKE @EmployeeCode";
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                sql += " AND e.Name LIKE @EmployeeName";
            if (!string.IsNullOrEmpty(filter.VendorCode))
                sql += " AND v.VendorCode LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.VendorName))
                sql += " AND v.VendorName LIKE @VendorName";
            if (!string.IsNullOrEmpty(filter.PAN))
                sql += " AND v.PanNumber LIKE @PAN";
            if (filter.TDSApplicableId.HasValue)
                sql += " AND mr.TDSApplicableId = @TDSApplicableId";
            if (filter.MinAmount.HasValue)
                sql += " AND mr.MonthlyLeaseAmount >= @MinAmount";
            if (filter.MaxAmount.HasValue)
                sql += " AND mr.MonthlyLeaseAmount <= @MaxAmount";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                sql += " AND mr.PaymentStatus = @PaymentStatus";
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                sql += " AND CAST(YEAR(mr.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2) = @FinancialYear";
            if (!string.IsNullOrEmpty(filter.Month))
                sql += " AND DATENAME(MONTH, mr.PaymentMonth) = @Month";
            if (!string.IsNullOrEmpty(filter.VoucherNumber))
                sql += " AND 'CN/' + RIGHT('000000' + CAST(mr.Id AS VARCHAR(6)), 6) + '/25-26' LIKE @VoucherNumber";
            if (!filter.IncludeZeroTDS)
                sql += " AND mr.TDSAmount > 0";

            sql += ") t";

            return sql;
        }

        private string BuildTDSReportQuery(TDSReportFilterModel filter)
        {
            var sql = @"
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY PaymentDate DESC) as SlNo,
                    'CN/' + RIGHT('000000' + CAST(mr.Id AS VARCHAR(6)), 6) + '/25-26' as VoucherNumber,
                    e.Code as EmployeeCode,
                    e.Name as EmployeeName,
                    v.VendorCode,
                    v.VendorName,
                    v.PanNumber as PAN,
                    PaymentDate,
                    MonthlyLeaseAmount as TDSDeductedOn,
                    TDSRate,
                    TDSAmount,
                    MonthlyLeaseAmount as GrossValue,
                    '' as ProjectName,
                    '70800 : Rent Paid (Rent Free Accommodation)' as CoAName,
                    'Rent Payable for the Month of ' + DATENAME(MONTH, PaymentMonth) + ' ' + CAST(YEAR(PaymentMonth) AS VARCHAR(4)) + 
                    ' on behalf of ' + e.Code + ': ' + e.Name + ' (@' + CAST(MonthlyLeaseAmount AS VARCHAR(20)) + '/-, ' + CAST(TDSRate AS VARCHAR(10)) + '%TDS deducted)' as Narration,
                    '' as ExemptionCertificateNo,
                    NULL as ExemptionFromDate,
                    NULL as ExemptionToDate,
                    'MonthlyRent' as PaymentType,
                    PaymentStatus,
                    l.RefNo as LeaseRefNo,
                    DATENAME(MONTH, PaymentMonth) as Month,
                    CAST(YEAR(PaymentMonth) AS VARCHAR(4)) as Year,
                    CAST(YEAR(PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT('0' + CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(2)), 2) as FinancialYear,
                    t.Name as TDSApplicableName,
                    '' as TransactionReference,
                    '' as Remarks,
                    mr.CreatedDate as CreatedDate,
                    '' as CreatedBy
                FROM MonthlyRentPayments mr
                INNER JOIN Employees e ON mr.EmployeeId = e.Id
                INNER JOIN Vendors v ON mr.VendorId = v.Id
                INNER JOIN Leases l ON mr.LeaseId = l.Id
                LEFT JOIN TDSApplicable t ON mr.TDSApplicableId = t.Id
                WHERE 1=1";

            // Add filters
            if (filter.FromDate.HasValue)
                sql += " AND mr.PaymentDate >= @FromDate";
            if (filter.ToDate.HasValue)
                sql += " AND mr.PaymentDate <= @ToDate";
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                sql += " AND e.Code LIKE @EmployeeCode";
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                sql += " AND e.Name LIKE @EmployeeName";
            if (!string.IsNullOrEmpty(filter.VendorCode))
                sql += " AND v.VendorCode LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.VendorName))
                sql += " AND v.VendorName LIKE @VendorName";
            if (!string.IsNullOrEmpty(filter.PAN))
                sql += " AND v.PanNumber LIKE @PAN";
            if (filter.TDSApplicableId.HasValue)
                sql += " AND mr.TDSApplicableId = @TDSApplicableId";
            if (filter.MinAmount.HasValue)
                sql += " AND mr.MonthlyLeaseAmount >= @MinAmount";
            if (filter.MaxAmount.HasValue)
                sql += " AND mr.MonthlyLeaseAmount <= @MaxAmount";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                sql += " AND mr.PaymentStatus = @PaymentStatus";
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                sql += " AND CAST(YEAR(mr.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2) = @FinancialYear";
            if (!string.IsNullOrEmpty(filter.Month))
                sql += " AND DATENAME(MONTH, mr.PaymentMonth) = @Month";
            if (!string.IsNullOrEmpty(filter.VoucherNumber))
                sql += " AND 'CN/' + RIGHT('000000' + CAST(mr.Id AS VARCHAR(6)), 6) + '/25-26' LIKE @VoucherNumber";

            if (!filter.IncludeZeroTDS)
                sql += " AND mr.TDSAmount > 0";

            // Add sorting
            sql += $" ORDER BY {filter.SortBy} {filter.SortOrder}";

            // Add pagination
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return sql;
        }

        private DynamicParameters BuildTDSReportParameters(TDSReportFilterModel filter)
        {
            var parameters = new DynamicParameters();
            
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);
            
            // Only add parameters if they have values to avoid SQL issues
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                parameters.Add("@EmployeeCode", $"%{filter.EmployeeCode}%");
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                parameters.Add("@EmployeeName", $"%{filter.EmployeeName}%");
            if (!string.IsNullOrEmpty(filter.VendorCode))
                parameters.Add("@VendorCode", $"%{filter.VendorCode}%");
            if (!string.IsNullOrEmpty(filter.VendorName))
                parameters.Add("@VendorName", $"%{filter.VendorName}%");
            if (!string.IsNullOrEmpty(filter.PAN))
                parameters.Add("@PAN", $"%{filter.PAN}%");
            if (filter.TDSApplicableId.HasValue)
                parameters.Add("@TDSApplicableId", filter.TDSApplicableId);
            if (filter.MinAmount.HasValue)
                parameters.Add("@MinAmount", filter.MinAmount);
            if (filter.MaxAmount.HasValue)
                parameters.Add("@MaxAmount", filter.MaxAmount);
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                parameters.Add("@PaymentStatus", filter.PaymentStatus);
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                parameters.Add("@FinancialYear", filter.FinancialYear);
            if (!string.IsNullOrEmpty(filter.Month))
                parameters.Add("@Month", filter.Month);
            if (!string.IsNullOrEmpty(filter.VoucherNumber))
                parameters.Add("@VoucherNumber", $"%{filter.VoucherNumber}%");

            return parameters;
        }

        private async Task<TDSReportSummary> CalculateTDSSummaryAsync(IDbConnection connection, TDSReportFilterModel filter)
        {
            var summarySql = @"
                SELECT 
                    COUNT(*) as TotalPayments,
                    SUM(MonthlyLeaseAmount) as TotalGrossAmount,
                    SUM(TDSAmount) as TotalTDSAmount,
                    SUM(MonthlyLeaseAmount - TDSAmount) as TotalNetAmount,
                    AVG(TDSRate) as AverageTDSRate,
                    COUNT(DISTINCT mr.EmployeeId) as UniqueEmployees,
                    COUNT(DISTINCT mr.VendorId) as UniqueVendors,
                    COUNT(DISTINCT v.PanNumber) as UniquePANs
                FROM MonthlyRentPayments mr
                INNER JOIN Employees e ON mr.EmployeeId = e.Id
                INNER JOIN Vendors v ON mr.VendorId = v.Id
                INNER JOIN Leases l ON mr.LeaseId = l.Id
                LEFT JOIN TDSApplicable t ON mr.TDSApplicableId = t.Id
                WHERE 1=1";

            // Add same filters as main query
            if (filter.FromDate.HasValue)
                summarySql += " AND mr.PaymentDate >= @FromDate";
            if (filter.ToDate.HasValue)
                summarySql += " AND mr.PaymentDate <= @ToDate";
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                summarySql += " AND e.Code LIKE @EmployeeCode";
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                summarySql += " AND e.Name LIKE @EmployeeName";
            if (!string.IsNullOrEmpty(filter.VendorCode))
                summarySql += " AND v.VendorCode LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.VendorName))
                summarySql += " AND v.VendorName LIKE @VendorName";
            if (!string.IsNullOrEmpty(filter.PAN))
                summarySql += " AND v.PanNumber LIKE @PAN";
            if (filter.TDSApplicableId.HasValue)
                summarySql += " AND mr.TDSApplicableId = @TDSApplicableId";
            if (filter.MinAmount.HasValue)
                summarySql += " AND mr.MonthlyLeaseAmount >= @MinAmount";
            if (filter.MaxAmount.HasValue)
                summarySql += " AND mr.MonthlyLeaseAmount <= @MaxAmount";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                summarySql += " AND mr.PaymentStatus = @PaymentStatus";
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                summarySql += " AND CAST(YEAR(mr.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2) = @FinancialYear";
            if (!string.IsNullOrEmpty(filter.Month))
                summarySql += " AND DATENAME(MONTH, mr.PaymentMonth) = @Month";
            if (!string.IsNullOrEmpty(filter.VoucherNumber))
                summarySql += " AND 'CN/' + RIGHT('000000' + CAST(mr.Id AS VARCHAR(6)), 6) + '/25-26' LIKE @VoucherNumber";
            if (!filter.IncludeZeroTDS)
                summarySql += " AND mr.TDSAmount > 0";

            var parameters = BuildTDSReportParameters(filter);

            var summary = await connection.QueryFirstOrDefaultAsync<TDSReportSummary>(summarySql, parameters);
            return summary ?? new TDSReportSummary();
        }

        // Helper method to test basic TDS query without filters
        public async Task<object> TestBasicTDSQueryAsync()
        {
            using var connection = CreateConnection();
            
            var testSql = @"
                SELECT TOP 5
                    mr.Id,
                    e.Code as EmployeeCode,
                    e.Name as EmployeeName,
                    v.VendorCode,
                    v.VendorName,
                    mr.PaymentDate,
                    mr.MonthlyLeaseAmount,
                    mr.TDSAmount,
                    mr.TDSRate,
                    mr.PaymentStatus
                FROM MonthlyRentPayments mr
                INNER JOIN Employees e ON mr.EmployeeId = e.Id
                INNER JOIN Vendors v ON mr.VendorId = v.Id
                INNER JOIN Leases l ON mr.LeaseId = l.Id
                ORDER BY mr.PaymentDate DESC";

            var results = await connection.QueryAsync(testSql);
            return new { TestResults = results.ToList() };
        }

        // Helper method to get TDS diagnostic information
        public async Task<object> GetTDSDiagnosticInfoAsync()
        {
            using var connection = CreateConnection();
            
            var diagnosticSql = @"
                SELECT 
                    (SELECT COUNT(*) FROM MonthlyRentPayments) as TotalMonthlyRentPayments,
                    (SELECT COUNT(*) FROM MonthlyRentPayments WHERE TDSAmount > 0) as TotalWithTDS,
                    (SELECT COUNT(*) FROM MonthlyRentPayments WHERE TDSAmount = 0 OR TDSAmount IS NULL) as TotalWithoutTDS,
                    (SELECT COUNT(*) FROM Employees WHERE IsActive = 1) as TotalEmployees,
                    (SELECT COUNT(*) FROM Vendors WHERE IsActive = 1) as TotalVendors,
                    (SELECT COUNT(*) FROM Leases WHERE IsActiveRecord = 1) as TotalLeases,
                    (SELECT COUNT(*) FROM TDSApplicable) as TotalTDSApplicable,
                    (SELECT MIN(PaymentDate) FROM MonthlyRentPayments) as EarliestPaymentDate,
                    (SELECT MAX(PaymentDate) FROM MonthlyRentPayments) as LatestPaymentDate,
                    (SELECT COUNT(*) FROM MonthlyRentPayments mr 
                     INNER JOIN Employees e ON mr.EmployeeId = e.Id 
                     INNER JOIN Vendors v ON mr.VendorId = v.Id 
                     INNER JOIN Leases l ON mr.LeaseId = l.Id) as TotalWithJoins";

            var result = await connection.QueryFirstOrDefaultAsync(diagnosticSql);
            
            return new
            {
                TotalMonthlyRentPayments = result?.TotalMonthlyRentPayments ?? 0,
                TotalWithTDS = result?.TotalWithTDS ?? 0,
                TotalWithoutTDS = result?.TotalWithoutTDS ?? 0,
                TotalEmployees = result?.TotalEmployees ?? 0,
                TotalVendors = result?.TotalVendors ?? 0,
                TotalLeases = result?.TotalLeases ?? 0,
                TotalTDSApplicable = result?.TotalTDSApplicable ?? 0,
                TotalWithJoins = result?.TotalWithJoins ?? 0,
                EarliestPaymentDate = result?.EarliestPaymentDate,
                LatestPaymentDate = result?.LatestPaymentDate,
                HasData = (result?.TotalMonthlyRentPayments ?? 0) > 0,
                HasTDSData = (result?.TotalWithTDS ?? 0) > 0,
                HasValidJoins = (result?.TotalWithJoins ?? 0) > 0
            };
        }

        // TDS Brokerage Report Export Methods
        public async Task<byte[]> ExportTDSBrokerageReportToPdfAsync(TDSBrokerageReportModel report, TDSBrokerageReportExportOptions options)
        {
            try
            {
                var html = new StringBuilder();
                
                // HTML header
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset='utf-8'>");
                html.AppendLine("<title>TDS Brokerage Report - Lease Brokerage</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; font-size: 12px; }");
                html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                html.AppendLine("th, td { border: 1px solid #ddd; padding: 6px; text-align: left; font-size: 11px; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".header { text-align: center; margin-bottom: 20px; }");
                html.AppendLine(".summary { margin-bottom: 20px; }");
                html.AppendLine(".filters { margin-bottom: 20px; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Report header
                html.AppendLine("<div class='header'>");
                html.AppendLine($"<h1>{options.ReportTitle}</h1>");
                html.AppendLine($"<p>Generated on: {report.GeneratedDate:dd/MM/yyyy HH:mm}</p>");
                html.AppendLine($"<p>Generated by: {report.GeneratedBy}</p>");
                html.AppendLine("</div>");

                // Summary section
                if (options.IncludeSummary)
                {
                    html.AppendLine("<div class='summary'>");
                    html.AppendLine("<h2>Summary</h2>");
                    html.AppendLine("<table>");
                    html.AppendLine("<tr><td>Total Payments:</td><td>" + report.Summary.TotalPayments + "</td></tr>");
                    html.AppendLine("<tr><td>Total Gross Amount:</td><td>₹" + report.Summary.TotalGrossAmount.ToString("N2") + "</td></tr>");
                    html.AppendLine("<tr><td>Total TDS Amount:</td><td>₹" + report.Summary.TotalTDSAmount.ToString("N2") + "</td></tr>");
                    html.AppendLine("<tr><td>Total Net Amount:</td><td>₹" + report.Summary.TotalNetAmount.ToString("N2") + "</td></tr>");
                    html.AppendLine("</table>");
                    html.AppendLine("</div>");
                }

                // Report data table
                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Sl No</th><th>Voucher No.</th><th>Code</th><th>Name</th><th>PAN</th>");
                html.AppendLine("<th>Date</th><th>TDS Deducted on</th><th>TDS %</th><th>TDS Amount</th><th>Gross CN Value</th>");
                html.AppendLine("<th>Project Name</th><th>CoA Name</th><th>Narration</th><th>Exemption Certificate No.</th><th>Exemption From Dt</th><th>Exemption To Dt</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");

                foreach (var item in report.TDSItems)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{item.SlNo}</td>");
                    html.AppendLine($"<td>{item.VoucherNumber}</td>");
                    html.AppendLine($"<td>{item.EmployeeCode}</td>");
                    html.AppendLine($"<td>{item.EmployeeName}</td>");
                    html.AppendLine($"<td>{item.PAN}</td>");
                    html.AppendLine($"<td>{item.PaymentDate:dd/MM/yyyy}</td>");
                    html.AppendLine($"<td>{item.TDSDeductedOn:N2}</td>");
                    html.AppendLine($"<td>{item.TDSRate:N2}</td>");
                    html.AppendLine($"<td>{item.TDSAmount:N2}</td>");
                    html.AppendLine($"<td>{item.GrossValue:N2}</td>");
                    html.AppendLine($"<td>{item.ProjectName}</td>");
                    html.AppendLine($"<td>{item.CoAName}</td>");
                    html.AppendLine($"<td>{item.Narration}</td>");
                    html.AppendLine($"<td>{item.ExemptionCertificateNo}</td>");
                    html.AppendLine($"<td>{(item.ExemptionFromDate.HasValue ? item.ExemptionFromDate.Value.ToString("dd/MM/yyyy") : "")}</td>");
                    html.AppendLine($"<td>{(item.ExemptionToDate.HasValue ? item.ExemptionToDate.Value.ToString("dd/MM/yyyy") : "")}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return System.Text.Encoding.UTF8.GetBytes(html.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating TDS Brokerage PDF report: {ex.Message}");
            }
        }

        public async Task<byte[]> ExportTDSBrokerageReportToExcelAsync(TDSBrokerageReportModel report, TDSBrokerageReportExportOptions options)
        {
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("TDS Brokerage Report");

                // Add header
                worksheet.Cell("A1").Value = options.ReportTitle;
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 16;
                worksheet.Range("A1:L1").Merge();

                // Add summary
                if (options.IncludeSummary)
                {
                    worksheet.Cell("A3").Value = "Summary";
                    worksheet.Cell("A3").Style.Font.Bold = true;
                    worksheet.Cell("A3").Style.Font.FontSize = 14;

                    worksheet.Cell("A4").Value = "Total Payments:";
                    worksheet.Cell("B4").Value = report.Summary.TotalPayments;
                    worksheet.Cell("A5").Value = "Total Gross Amount:";
                    worksheet.Cell("B5").Value = report.Summary.TotalGrossAmount;
                    worksheet.Cell("A6").Value = "Total TDS Amount:";
                    worksheet.Cell("B6").Value = report.Summary.TotalTDSAmount;
                    worksheet.Cell("A7").Value = "Total Net Amount:";
                    worksheet.Cell("B7").Value = report.Summary.TotalNetAmount;
                }

                // Add table headers
                var headerRow = options.IncludeSummary ? 9 : 3;
                worksheet.Cell($"A{headerRow}").Value = "Sl No";
                worksheet.Cell($"B{headerRow}").Value = "Voucher No.";
                worksheet.Cell($"C{headerRow}").Value = "Code";
                worksheet.Cell($"D{headerRow}").Value = "Name";
                worksheet.Cell($"E{headerRow}").Value = "PAN";
                worksheet.Cell($"F{headerRow}").Value = "Date";
                worksheet.Cell($"G{headerRow}").Value = "TDS Deducted on";
                worksheet.Cell($"H{headerRow}").Value = "TDS %";
                worksheet.Cell($"I{headerRow}").Value = "TDS Amount";
                worksheet.Cell($"J{headerRow}").Value = "Gross CN Value";
                worksheet.Cell($"K{headerRow}").Value = "Project Name";
                worksheet.Cell($"L{headerRow}").Value = "CoA Name";
                worksheet.Cell($"M{headerRow}").Value = "Narration";
                worksheet.Cell($"N{headerRow}").Value = "Exemption Certificate No.";
                worksheet.Cell($"O{headerRow}").Value = "Exemption From Dt";
                worksheet.Cell($"P{headerRow}").Value = "Exemption To Dt";

                // Style headers
                var headerRange = worksheet.Range($"A{headerRow}:P{headerRow}");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                // Add data
                var dataStartRow = headerRow + 1;
                for (int i = 0; i < report.TDSItems.Count; i++)
                {
                    var item = report.TDSItems[i];
                    var row = dataStartRow + i;

                    worksheet.Cell($"A{row}").Value = item.SlNo;
                    worksheet.Cell($"B{row}").Value = item.VoucherNumber;
                    worksheet.Cell($"C{row}").Value = item.EmployeeCode;
                    worksheet.Cell($"D{row}").Value = item.EmployeeName;
                    worksheet.Cell($"E{row}").Value = item.PAN;
                    worksheet.Cell($"F{row}").Value = item.PaymentDate.ToString("dd/MM/yyyy");
                    worksheet.Cell($"G{row}").Value = item.TDSDeductedOn;
                    worksheet.Cell($"H{row}").Value = item.TDSRate;
                    worksheet.Cell($"I{row}").Value = item.TDSAmount;
                    worksheet.Cell($"J{row}").Value = item.GrossValue;
                    worksheet.Cell($"K{row}").Value = item.ProjectName;
                    worksheet.Cell($"L{row}").Value = item.CoAName;
                    worksheet.Cell($"M{row}").Value = item.Narration;
                    worksheet.Cell($"N{row}").Value = item.ExemptionCertificateNo;
                    worksheet.Cell($"O{row}").Value = item.ExemptionFromDate?.ToString("dd/MM/yyyy");
                    worksheet.Cell($"P{row}").Value = item.ExemptionToDate?.ToString("dd/MM/yyyy");
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating TDS Brokerage Excel report: {ex.Message}");
            }
        }

        public async Task<string> ExportTDSBrokerageReportToCsvAsync(TDSBrokerageReportModel report, TDSBrokerageReportExportOptions options)
        {
            try
            {
                var csv = new StringBuilder();

                // Add headers
                csv.AppendLine("Sl No,Voucher No,Employee Code,Employee Name,Vendor Code,Vendor Name,PAN,Payment Date,Gross Amount,TDS Rate,TDS Amount,Net Amount,Status,Lease Ref,Month,Year,Financial Year");

                // Add data
                foreach (var item in report.TDSItems)
                {
                    csv.AppendLine($"{item.SlNo}," +
                                 $"\"{item.VoucherNumber}\"," +
                                 $"\"{item.EmployeeCode}\"," +
                                 $"\"{item.EmployeeName}\"," +
                                 $"\"{item.VendorCode}\"," +
                                 $"\"{item.VendorName}\"," +
                                 $"\"{item.PAN}\"," +
                                 $"\"{item.PaymentDate:dd/MM/yyyy}\"," +
                                 $"{item.GrossValue}," +
                                 $"{item.TDSRate}," +
                                 $"{item.TDSAmount}," +
                                 $"{item.NetPayableAmount}," +
                                 $"\"{item.PaymentStatus}\"," +
                                 $"\"{item.LeaseRefNo}\"," +
                                 $"\"{item.Month}\"," +
                                 $"\"{item.Year}\"," +
                                 $"\"{item.FinancialYear}\"");
                }

                return csv.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating TDS Brokerage CSV report: {ex.Message}");
            }
        }

        // Helper method to calculate TDS Brokerage summary
        private async Task<TDSBrokerageReportSummary> CalculateTDSBrokerageSummaryAsync(IDbConnection connection, TDSBrokerageReportFilterModel filter)
        {
            var summarySql = @"
                SELECT 
                    COUNT(*) as TotalPayments,
                    SUM(ISNULL(bp.BrokerageAmount, 0)) as TotalGrossAmount,
                    SUM(ISNULL(TDSAmount, 0)) as TotalTDSAmount,
                    SUM(ISNULL(NetPayableAmount, 0)) as TotalNetAmount,
                    AVG(ISNULL(TDSRate, 0)) as AverageTDSRate,
                    COUNT(DISTINCT bp.EmployeeId) as UniqueEmployees,
                    COUNT(DISTINCT bp.VendorId) as UniqueVendors,
                    COUNT(DISTINCT v.PanNumber) as UniquePANs
                FROM BrokeragePayment bp
                INNER JOIN Employees e ON bp.EmployeeId = e.Id
                INNER JOIN Vendors v ON bp.VendorId = v.Id
                INNER JOIN Leases l ON bp.LeaseId = l.Id
                WHERE 1=1";

            // Add filters
            if (filter.FromDate.HasValue)
                summarySql += " AND bp.PaymentDate >= @FromDate";
            if (filter.ToDate.HasValue)
                summarySql += " AND bp.PaymentDate <= @ToDate";
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                summarySql += " AND e.Code LIKE @EmployeeCode";
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                summarySql += " AND e.Name LIKE @EmployeeName";
            if (!string.IsNullOrEmpty(filter.VendorCode))
                summarySql += " AND v.VendorCode LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.VendorName))
                summarySql += " AND v.VendorName LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.PAN))
                summarySql += " AND v.PanNumber LIKE @PAN";
            if (filter.TDSApplicableId.HasValue)
                summarySql += " AND bp.TDSApplicableId = @TDSApplicableId";
            if (filter.MinAmount.HasValue)
                summarySql += " AND bp.BrokerageAmount >= @MinAmount";
            if (filter.MaxAmount.HasValue)
                summarySql += " AND bp.BrokerageAmount <= @MaxAmount";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                summarySql += " AND bp.PaymentStatus = @PaymentStatus";
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                summarySql += " AND CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2) = @FinancialYear";
            if (!string.IsNullOrEmpty(filter.Month))
                summarySql += " AND DATENAME(MONTH, bp.PaymentMonth) = @Month";
            if (!filter.IncludeZeroTDS)
                summarySql += " AND bp.TDSAmount > 0";

            var parameters = BuildTDSBrokerageReportParameters(filter);
            var summary = await connection.QueryFirstOrDefaultAsync<TDSBrokerageReportSummary>(summarySql, parameters);
            return summary ?? new TDSBrokerageReportSummary();
        }

        // Helper method to test basic TDS Brokerage query without filters
        public async Task<bool> TestBasicTDSBrokerageQueryAsync()
        {
            try
            {
                using var connection = CreateConnection();
                
                var testSql = @"
                    SELECT TOP 5
                        bp.Id,
                        e.Code as EmployeeCode,
                        e.Name as EmployeeName,
                        v.VendorCode,
                        v.VendorName,
                        bp.PaymentDate,
                        bp.BrokerageAmount,
                        bp.TDSAmount,
                        bp.TDSRate,
                        bp.PaymentStatus
                    FROM BrokeragePayment bp
                    INNER JOIN Employees e ON bp.EmployeeId = e.Id
                    INNER JOIN Vendors v ON bp.VendorId = v.Id
                    INNER JOIN Leases l ON bp.LeaseId = l.Id
                    ORDER BY bp.PaymentDate DESC";

                var results = await connection.QueryAsync(testSql);
                
                // Log the results for debugging
                Console.WriteLine("=== BASIC TDS BROKERAGE QUERY TEST ===");
                foreach (var item in results)
                {
                    Console.WriteLine($"ID: {item.Id}, BrokerageAmount: {item.BrokerageAmount}, TDSRate: {item.TDSRate}, TDSAmount: {item.TDSAmount}");
                }
                
                return results.Any(); // Return true if we got results, false otherwise
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TestBasicTDSBrokerageQueryAsync: {ex.Message}");
                return false;
            }
        }

        // Helper method to get TDS Brokerage diagnostic information
        public async Task<object> GetTDSBrokerageDiagnosticInfoAsync()
        {
            using var connection = CreateConnection();
            
            var diagnosticSql = @"
                SELECT 
                    (SELECT COUNT(*) FROM BrokeragePayment) as TotalBrokeragePayments,
                    (SELECT COUNT(*) FROM BrokeragePayment WHERE TDSAmount > 0) as TotalWithTDS,
                    (SELECT COUNT(*) FROM BrokeragePayment WHERE TDSAmount = 0 OR TDSAmount IS NULL) as TotalWithoutTDS,
                    (SELECT COUNT(*) FROM Employees WHERE IsActive = 1) as TotalEmployees,
                    (SELECT COUNT(*) FROM Vendors WHERE IsActive = 1) as TotalVendors,
                    (SELECT COUNT(*) FROM Leases WHERE IsActiveRecord = 1) as TotalLeases,
                    (SELECT COUNT(*) FROM TDSApplicable) as TotalTDSApplicable,
                    (SELECT MIN(PaymentDate) FROM BrokeragePayment) as EarliestPaymentDate,
                    (SELECT MAX(PaymentDate) FROM BrokeragePayment) as LatestPaymentDate,
                    (SELECT COUNT(*) FROM BrokeragePayment bp 
                     INNER JOIN Employees e ON bp.EmployeeId = e.Id 
                     INNER JOIN Vendors v ON bp.VendorId = v.Id 
                     INNER JOIN Leases l ON bp.LeaseId = l.Id) as TotalWithJoins";

            var result = await connection.QueryFirstOrDefaultAsync(diagnosticSql);
            
            return new
            {
                TotalBrokeragePayments = result?.TotalBrokeragePayments ?? 0,
                TotalWithTDS = result?.TotalWithTDS ?? 0,
                TotalWithoutTDS = result?.TotalWithoutTDS ?? 0,
                TotalEmployees = result?.TotalEmployees ?? 0,
                TotalVendors = result?.TotalVendors ?? 0,
                TotalLeases = result?.TotalLeases ?? 0,
                TotalTDSApplicable = result?.TotalTDSApplicable ?? 0,
                TotalWithJoins = result?.TotalWithJoins ?? 0,
                EarliestPaymentDate = result?.EarliestPaymentDate,
                LatestPaymentDate = result?.LatestPaymentDate,
                HasData = (result?.TotalBrokeragePayments ?? 0) > 0,
                HasTDSData = (result?.TotalWithTDS ?? 0) > 0,
                HasValidJoins = (result?.TotalWithJoins ?? 0) > 0
            };
        }

        // Helper methods for TDS Brokerage Report queries
        private string BuildTDSBrokerageReportCountQuery(TDSBrokerageReportFilterModel filter)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM BrokeragePayment bp
                INNER JOIN Employees e ON bp.EmployeeId = e.Id
                INNER JOIN Vendors v ON bp.VendorId = v.Id
                INNER JOIN Leases l ON bp.LeaseId = l.Id
                LEFT JOIN TDSApplicable t ON bp.TDSApplicableId = t.Id
                WHERE 1=1";

            // Add filters
            if (filter.FromDate.HasValue)
                sql += " AND bp.PaymentDate >= @FromDate";
            if (filter.ToDate.HasValue)
                sql += " AND bp.PaymentDate <= @ToDate";
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                sql += " AND e.Code LIKE @EmployeeCode";
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                sql += " AND e.Name LIKE @EmployeeName";
            if (!string.IsNullOrEmpty(filter.VendorCode))
                sql += " AND v.VendorCode LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.VendorName))
                sql += " AND v.VendorName LIKE @VendorName";
            if (!string.IsNullOrEmpty(filter.PAN))
                sql += " AND v.PanNumber LIKE @PAN";
            if (filter.TDSApplicableId.HasValue)
                sql += " AND bp.TDSApplicableId = @TDSApplicableId";
            if (filter.MinAmount.HasValue)
                sql += " AND bp.BrokerageAmount >= @MinAmount";
            if (filter.MaxAmount.HasValue)
                sql += " AND bp.BrokerageAmount <= @MaxAmount";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                sql += " AND bp.PaymentStatus = @PaymentStatus";
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                sql += " AND CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2) = @FinancialYear";
            if (!string.IsNullOrEmpty(filter.Month))
                sql += " AND DATENAME(MONTH, bp.PaymentMonth) = @Month";
            if (!filter.IncludeZeroTDS)
                sql += " AND bp.TDSAmount > 0";

            return sql;
        }

        private string BuildTDSBrokerageReportQuery(TDSBrokerageReportFilterModel filter)
        {
            var sql = @"
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY bp.PaymentDate DESC) as SlNo,
                    'CN/' + RIGHT('000000' + CAST(bp.Id AS VARCHAR(6)), 6) + '/25-26' as VoucherNumber,
                    e.Code as EmployeeCode,
                    e.Name as EmployeeName,
                    v.VendorCode,
                    v.VendorName,
                    v.PanNumber as PAN,
                    bp.PaymentDate as PaymentDate,
                    CAST(bp.BrokerageAmount AS DECIMAL(18,2)) as TDSDeductedOn,
                    CAST(bp.TDSRate AS DECIMAL(5,2)) as TDSRate,
                    CAST(bp.TDSAmount AS DECIMAL(18,2)) as TDSAmount,
                    CAST(bp.BrokerageAmount AS DECIMAL(18,2)) as GrossValue,
                    '' as ProjectName,
                    '70800 : Rent Paid (Rent Free Accommodation)' as CoAName,
                    'Brokerage Payment for the Month of ' + DATENAME(MONTH, bp.PaymentMonth) + ' ' + CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)) + 
                    ' on behalf of ' + e.Code + ': ' + e.Name + ' (@' + CAST(bp.BrokerageAmount AS VARCHAR(20)) + '/-, ' + CAST(bp.TDSRate AS VARCHAR(10)) + '%TDS deducted)' as Narration,
                    '' as ExemptionCertificateNo,
                    NULL as ExemptionFromDate,
                    NULL as ExemptionToDate,
                    'Brokerage' as PaymentType,
                    bp.PaymentStatus,
                    l.RefNo as LeaseRefNo,
                    DATENAME(MONTH, bp.PaymentMonth) as Month,
                    CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)) as Year,
                    CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT('0' + CAST(YEAR(bp.PaymentMonth) + 1 AS VARCHAR(2)), 2) as FinancialYear,
                    t.Name as TDSApplicableName,
                    '' as TransactionReference,
                    '' as Remarks,
                    bp.CreatedDate as CreatedDate,
                    '' as CreatedBy,
                    CAST(bp.NetPayableAmount AS DECIMAL(18,2)) as NetPayableAmount,
                    bp.DSCApprovalStatus
                FROM BrokeragePayment bp
                INNER JOIN Employees e ON bp.EmployeeId = e.Id
                INNER JOIN Vendors v ON bp.VendorId = v.Id
                INNER JOIN Leases l ON bp.LeaseId = l.Id
                LEFT JOIN TDSApplicable t ON bp.TDSApplicableId = t.Id
                WHERE 1=1";

            // Add filters
            if (filter.FromDate.HasValue)
                sql += " AND bp.PaymentDate >= @FromDate";
            if (filter.ToDate.HasValue)
                sql += " AND bp.PaymentDate <= @ToDate";
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                sql += " AND e.Code LIKE @EmployeeCode";
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                sql += " AND e.Name LIKE @EmployeeName";
            if (!string.IsNullOrEmpty(filter.VendorCode))
                sql += " AND v.VendorCode LIKE @VendorCode";
            if (!string.IsNullOrEmpty(filter.VendorName))
                sql += " AND v.VendorName LIKE @VendorName";
            if (!string.IsNullOrEmpty(filter.PAN))
                sql += " AND v.PanNumber LIKE @PAN";
            if (filter.TDSApplicableId.HasValue)
                sql += " AND bp.TDSApplicableId = @TDSApplicableId";
            if (filter.MinAmount.HasValue)
                sql += " AND bp.BrokerageAmount >= @MinAmount";
            if (filter.MaxAmount.HasValue)
                sql += " AND bp.BrokerageAmount <= @MaxAmount";
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                sql += " AND bp.PaymentStatus = @PaymentStatus";
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                sql += " AND CAST(YEAR(bp.PaymentMonth) AS VARCHAR(4)) + '-' + RIGHT(CAST(YEAR(PaymentMonth) + 1 AS VARCHAR(4)), 2) = @FinancialYear";
            if (!string.IsNullOrEmpty(filter.Month))
                sql += " AND DATENAME(MONTH, bp.PaymentMonth) = @Month";
            if (!filter.IncludeZeroTDS)
                sql += " AND bp.TDSAmount > 0";

            // Add sorting
            sql += $" ORDER BY {filter.SortBy} {filter.SortOrder}";

            // Add pagination
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            return sql;
        }

        private DynamicParameters BuildTDSBrokerageReportParameters(TDSBrokerageReportFilterModel filter)
        {
            var parameters = new DynamicParameters();
            
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);
            
            // Only add parameters if they have values to avoid SQL issues
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                parameters.Add("@EmployeeCode", $"%{filter.EmployeeCode}%");
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                parameters.Add("@EmployeeName", $"%{filter.EmployeeName}%");
            if (!string.IsNullOrEmpty(filter.VendorCode))
                parameters.Add("@VendorCode", $"%{filter.VendorCode}%");
            if (!string.IsNullOrEmpty(filter.VendorName))
                parameters.Add("@VendorName", $"%{filter.VendorName}%");
            if (!string.IsNullOrEmpty(filter.PAN))
                parameters.Add("@PAN", $"%{filter.PAN}%");
            if (filter.TDSApplicableId.HasValue)
                parameters.Add("@TDSApplicableId", filter.TDSApplicableId);
            if (filter.MinAmount.HasValue)
                parameters.Add("@MinAmount", filter.MinAmount);
            if (filter.MaxAmount.HasValue)
                parameters.Add("@MinAmount", filter.MaxAmount);
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                parameters.Add("@PaymentStatus", filter.PaymentStatus);
            if (!string.IsNullOrEmpty(filter.FinancialYear))
                parameters.Add("@FinancialYear", filter.FinancialYear);
            if (!string.IsNullOrEmpty(filter.Month))
                parameters.Add("@Month", filter.Month);

            return parameters;
        }

        #region Rent Payment Reports

        public async Task<RentPaymentReportModel> GetRentPaymentReportAsync(RentPaymentReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var sql = BuildRentPaymentReportQuery(filter);
            var parameters = BuildRentPaymentReportParameters(filter);

            var items = await connection.QueryAsync<RentPaymentItem>(sql, parameters);
            var itemList = items.ToList();

            // Get total count
            var countSql = BuildRentPaymentReportCountQuery(filter);
            var totalRecords = await connection.QuerySingleAsync<int>(countSql, parameters);

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
                GeneratedBy = "System",
                Filter = filter,
                PaymentItems = itemList,
                Summary = summary,
                TotalRecords = totalRecords,
                ReportType = "RentPayment",
                ReportPeriod = $"{filter.FromDate?.ToString("MMM yyyy")} - {filter.ToDate?.ToString("MMM yyyy")}"
            };
        }

        public async Task<RentPaymentReportModel> GetRentPaymentMonthlyReportAsync(RentPaymentReportFilterModel filter)
        {
            return await GetRentPaymentReportAsync(filter);
        }

        public async Task<RentPaymentReportModel> GetRentPaymentVendorReportAsync(RentPaymentReportFilterModel filter)
        {
            return await GetRentPaymentReportAsync(filter);
        }

        public async Task<RentPaymentReportModel> GetRentPaymentEmployeeReportAsync(RentPaymentReportFilterModel filter)
        {
            return await GetRentPaymentReportAsync(filter);
        }

        public async Task<RentPaymentChartData> GetRentPaymentChartDataAsync(RentPaymentReportFilterModel filter)
        {
            return new RentPaymentChartData();
        }

        public async Task<RentPaymentPeggingModel> GetRentPaymentPeggingAsync(int paymentId, string paymentType)
        {
            return new RentPaymentPeggingModel();
        }

        public async Task<byte[]> ExportRentPaymentReportToPdfAsync(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            var html = GenerateRentPaymentReportHtml(report, options);
            return System.Text.Encoding.UTF8.GetBytes(html);
        }

        public async Task<byte[]> ExportRentPaymentReportToExcelAsync(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            return new byte[0];
        }

        public async Task<string> ExportRentPaymentReportToCsvAsync(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Sl No,Payment No,Transaction No,Vendor Name,Bank Name,Account No,PAN,Invoice No,Amount,Payable,Employee Name,Remarks");
            
            foreach (var item in report.PaymentItems)
            {
                csv.AppendLine($"{item.SlNo},{item.PaymentNumber},{item.TransactionNumber},{item.VendorName},{item.BankName},{item.AccountNumber},{item.PAN},{item.InvoiceNumber},{item.Amount},{item.Payable},{item.EmployeeName},{item.Remarks}");
            }
            
            return csv.ToString();
        }

        public async Task<object> GetRentPaymentDiagnosticInfoAsync()
        {
            using var connection = CreateConnection();
            
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

        public async Task<object> TestBasicRentPaymentQueryAsync()
        {
            using var connection = CreateConnection();
            
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

        private async Task<RentPaymentSummary> CalculateRentPaymentSummaryAsync(RentPaymentReportFilterModel filter)
        {
            using var connection = CreateConnection();
            
            var whereClause = BuildRentPaymentWhereClause(filter);
            var parameters = BuildRentPaymentReportParameters(filter);

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

            var result = await connection.QuerySingleAsync(sql, parameters);
            
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

        private string BuildRentPaymentReportQuery(RentPaymentReportFilterModel filter)
        {
            var whereClause = BuildRentPaymentWhereClause(filter);
            var orderClause = BuildRentPaymentOrderClause(filter);
            var paginationClause = BuildRentPaymentPaginationClause(filter);

            return $@"
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
        }

        private string BuildRentPaymentReportCountQuery(RentPaymentReportFilterModel filter)
        {
            var whereClause = BuildRentPaymentWhereClause(filter);

            return $@"
                SELECT COUNT(*)
                FROM MonthlyRentPayments mrp
                LEFT JOIN Employees e ON mrp.EmployeeId = e.Id
                LEFT JOIN Vendors v ON mrp.VendorId = v.Id
                {whereClause}";
        }

        private string BuildRentPaymentWhereClause(RentPaymentReportFilterModel filter)
        {
            var conditions = new List<string>();

            if (filter.FromDate.HasValue)
                conditions.Add("mrp.PaymentDate >= @FromDate");

            if (filter.ToDate.HasValue)
                conditions.Add("mrp.PaymentDate <= @ToDate");

            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                conditions.Add("e.Code LIKE @EmployeeCode");

            if (!string.IsNullOrEmpty(filter.EmployeeName))
                conditions.Add("e.Name LIKE @EmployeeName");

            if (!string.IsNullOrEmpty(filter.VendorCode))
                conditions.Add("v.VendorCode LIKE @VendorCode");

            if (!string.IsNullOrEmpty(filter.VendorName))
                conditions.Add("v.VendorName LIKE @VendorName");

            if (!string.IsNullOrEmpty(filter.PAN))
                conditions.Add("v.PANNumber LIKE @PAN");

            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                conditions.Add("mrp.PaymentStatus = @PaymentStatus");

            if (!string.IsNullOrEmpty(filter.Month))
                conditions.Add("mrp.PaymentMonth = @Month");

            if (!string.IsNullOrEmpty(filter.BankName))
                conditions.Add("v.BankName LIKE @BankName");

            if (filter.MinAmount.HasValue)
                conditions.Add("mrp.Amount >= @MinAmount");

            if (filter.MaxAmount.HasValue)
                conditions.Add("mrp.Amount <= @MaxAmount");

            if (!filter.IncludeZeroAmounts)
                conditions.Add("mrp.Amount > 0");

            return conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
        }

        private string BuildRentPaymentOrderClause(RentPaymentReportFilterModel filter)
        {
            var sortBy = filter.SortBy ?? "PaymentDate";
            var sortOrder = filter.SortOrder ?? "DESC";

            return $"ORDER BY mrp.{sortBy} {sortOrder}";
        }

        private string BuildRentPaymentPaginationClause(RentPaymentReportFilterModel filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var offset = (filter.Page - 1) * filter.PageSize;
            return $"OFFSET {offset} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";
        }

        private DynamicParameters BuildRentPaymentReportParameters(RentPaymentReportFilterModel filter)
        {
            var parameters = new DynamicParameters();
            
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);
            
            if (!string.IsNullOrEmpty(filter.EmployeeCode))
                parameters.Add("@EmployeeCode", $"%{filter.EmployeeCode}%");
            if (!string.IsNullOrEmpty(filter.EmployeeName))
                parameters.Add("@EmployeeName", $"%{filter.EmployeeName}%");
            if (!string.IsNullOrEmpty(filter.VendorCode))
                parameters.Add("@VendorCode", $"%{filter.VendorCode}%");
            if (!string.IsNullOrEmpty(filter.VendorName))
                parameters.Add("@VendorName", $"%{filter.VendorName}%");
            if (!string.IsNullOrEmpty(filter.PAN))
                parameters.Add("@PAN", $"%{filter.PAN}%");
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                parameters.Add("@PaymentStatus", filter.PaymentStatus);
            if (!string.IsNullOrEmpty(filter.Month))
                parameters.Add("@Month", filter.Month);
            if (!string.IsNullOrEmpty(filter.BankName))
                parameters.Add("@BankName", $"%{filter.BankName}%");
            if (filter.MinAmount.HasValue)
                parameters.Add("@MinAmount", filter.MinAmount);
            if (filter.MaxAmount.HasValue)
                parameters.Add("@MaxAmount", filter.MaxAmount);

            return parameters;
        }

        private string GenerateRentPaymentReportHtml(RentPaymentReportModel report, RentPaymentReportExportOptions options)
        {
            return "<html><body><h1>Rent Payment Report</h1></body></html>";
        }

        #endregion
    }
}
