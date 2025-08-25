using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using RentManagement.Models;
using RentManagement.Data;
using System.Data.SqlClient;
using System;
using System.Linq;

namespace RentManagement.Data
{
    public class MonthlyRentPaymentRepository : IMonthlyRentPaymentRepository
    {
        private readonly string _connectionString;

        public MonthlyRentPaymentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);

        #region Basic CRUD Operations

        public async Task<int> CreateAsync(MonthlyRentPayment payment)
        {
            using var connection = CreateConnection();

            var sql = @"
                INSERT INTO MonthlyRentPayments (
                    LeaseId, EmployeeId, VendorId, PaymentMonth, MonthlyLeaseAmount,
                    TDSApplicableId, TDSRate, TDSAmount, NetPayableAmount, LeaseEndDate,
                    PaymentStatus, DSCApprovalStatus, ApprovedBy, ApprovedDate, PaymentDate,
                    TransactionReference, Remark, CreatedBy, CreatedDate, IsActive,
                    ApprovalStatus, MakerUserId, MakerUserName, MakerAction, 
                    CheckerUserId, CheckerUserName, CheckerApprovalDate, RejectionReason, IsActiveRecord
                )
                VALUES (
                    @LeaseId, @EmployeeId, @VendorId, @PaymentMonth, @MonthlyLeaseAmount,
                    @TDSApplicableId, @TDSRate, @TDSAmount, @NetPayableAmount, @LeaseEndDate,
                    @PaymentStatus, @DSCApprovalStatus, @ApprovedBy, @ApprovedDate, @PaymentDate,
                    @TransactionReference, @Remark, @CreatedBy, GETDATE(), @IsActive,
                    @ApprovalStatus, @MakerUserId, @MakerUserName, @MakerAction,
                    @CheckerUserId, @CheckerUserName, @CheckerApprovalDate, @RejectionReason, @IsActiveRecord
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                payment.LeaseId,
                payment.EmployeeId,
                payment.VendorId,
                payment.PaymentMonth,
                payment.MonthlyLeaseAmount,
                payment.TDSApplicableId,
                payment.TDSRate,
                payment.TDSAmount,
                payment.NetPayableAmount,
                payment.LeaseEndDate,
                PaymentStatus = payment.PaymentStatus ?? "Pending",
                DSCApprovalStatus = payment.DSCApprovalStatus ?? "Pending",
                payment.ApprovedBy,
                payment.ApprovedDate,
                payment.PaymentDate,
                payment.TransactionReference,
                payment.Remark,
                payment.CreatedBy,
                IsActive = payment.IsActive,
                ApprovalStatus = (int)payment.ApprovalStatus,
                payment.MakerUserId,
                payment.MakerUserName,
                MakerAction = (int)payment.MakerAction,
                payment.CheckerUserId,
                payment.CheckerUserName,
                payment.CheckerApprovalDate,
                payment.RejectionReason,
                IsActiveRecord = payment.IsActiveRecord
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<MonthlyRentPayment?> GetByIdAsync(int id)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT p.*, 
                       e.Name as EmployeeName, 
                       v.VendorName, 
                       l.RefNo as LeaseName,
                       t.Name as TDSApplicableName
                FROM MonthlyRentPayments p
                LEFT JOIN Employees e ON p.EmployeeId = e.Id
                LEFT JOIN Vendors v ON p.VendorId = v.Id
                LEFT JOIN Leases l ON p.LeaseId = l.Id
                LEFT JOIN TDSApplicable t ON p.TDSApplicableId = t.Id
                WHERE p.Id = @Id";

            return await connection.QueryFirstOrDefaultAsync<MonthlyRentPayment>(sql, new { Id = id });
        }

        // New method to get all payments with filters
        public async Task<IEnumerable<MonthlyRentPayment>> GetAllPaymentsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT p.*, e.Name as EmployeeName, v.VendorName, l.RefNo as LeaseName
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY Id DESC, CreatedDate DESC) as RowNum
                    FROM MonthlyRentPayments 
                    WHERE IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                           OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%')
                           OR MakerUserName LIKE '%' + @SearchTerm + '%')
                    AND (@StatusFilter IS NULL OR @StatusFilter = '' OR PaymentStatus = @StatusFilter)
                ) AS p
                LEFT JOIN Employees e ON p.EmployeeId = e.Id
                LEFT JOIN Vendors v ON p.VendorId = v.Id
                LEFT JOIN Leases l ON p.LeaseId = l.Id
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<MonthlyRentPayment>(sql, parameters);
        }

        public async Task<int> GetAllPaymentsCountAsync(string searchTerm, string statusFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM MonthlyRentPayments p
                WHERE IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = p.EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                       OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = p.VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%')
                       OR MakerUserName LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter IS NULL OR @StatusFilter = '' OR PaymentStatus = @StatusFilter)";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<PagedResult<MonthlyRentPayment>> GetAllAsync(int page, int pageSize, string? search)
        {
            using var connection = CreateConnection();

            var sql = @"
                WITH PaymentsCTE AS (
                    SELECT p.*, 
                           e.Name as EmployeeName, 
                           v.VendorName, 
                           l.RefNo as LeaseName,
                           t.Name as TDSApplicableName,
                           ROW_NUMBER() OVER (ORDER BY p.Id DESC, p.CreatedDate DESC) as RowNum
                    FROM MonthlyRentPayments p
                    LEFT JOIN Employees e ON p.EmployeeId = e.Id
                    LEFT JOIN Vendors v ON p.VendorId = v.Id
                    LEFT JOIN Leases l ON p.LeaseId = l.Id
                    LEFT JOIN TDSApplicable t ON p.TDSApplicableId = t.Id
                    WHERE p.IsActiveRecord = 1
                    AND (@Search IS NULL OR @Search = '' 
                         OR e.Name LIKE '%' + @Search + '%'
                         OR v.VendorName LIKE '%' + @Search + '%'
                         OR l.RefNo LIKE '%' + @Search + '%')
                )
                SELECT * FROM PaymentsCTE
                WHERE RowNum BETWEEN ((@Page - 1) * @PageSize + 1) AND (@Page * @PageSize);
                
                SELECT COUNT(*)
                FROM MonthlyRentPayments p
                LEFT JOIN Employees e ON p.EmployeeId = e.Id
                LEFT JOIN Vendors v ON p.VendorId = v.Id
                LEFT JOIN Leases l ON p.LeaseId = l.Id
                WHERE p.IsActiveRecord = 1
                AND (@Search IS NULL OR @Search = '' 
                     OR e.Name LIKE '%' + @Search + '%'
                     OR v.VendorName LIKE '%' + @Search + '%'
                     OR l.RefNo LIKE '%' + @Search + '%');";

            using var multi = await connection.QueryMultipleAsync(sql, new { Page = page, PageSize = pageSize, Search = search });

            var payments = (await multi.ReadAsync<MonthlyRentPayment>()).ToList();
            var totalCount = await multi.ReadFirstAsync<int>();

            return new PagedResult<MonthlyRentPayment>
            {
                Items = payments,
                TotalItems = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateAsync(MonthlyRentPayment payment)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE MonthlyRentPayments
                SET 
                    LeaseId = @LeaseId,
                    EmployeeId = @EmployeeId,
                    VendorId = @VendorId,
                    PaymentMonth = @PaymentMonth,
                    MonthlyLeaseAmount = @MonthlyLeaseAmount,
                    TDSApplicableId = @TDSApplicableId,
                    TDSRate = @TDSRate,
                    TDSAmount = @TDSAmount,
                    NetPayableAmount = @NetPayableAmount,
                    LeaseEndDate = @LeaseEndDate,
                    PaymentStatus = @PaymentStatus,
                    DSCApprovalStatus = @DSCApprovalStatus,
                    Remark = @Remark,
                    ModifiedBy = @ModifiedBy,
                    ModifiedDate = GETDATE(),
                    ApprovalStatus = @ApprovalStatus,
                    MakerUserId = @MakerUserId,
                    MakerUserName = @MakerUserName,
                    MakerAction = @MakerAction,
                    CheckerUserId = @CheckerUserId,
                    CheckerUserName = @CheckerUserName,
                    CheckerApprovalDate = @CheckerApprovalDate,
                    RejectionReason = @RejectionReason,
                    IsActiveRecord = @IsActiveRecord
                WHERE Id = @Id";

            var parameters = new
            {
                payment.Id,
                payment.LeaseId,
                payment.EmployeeId,
                payment.VendorId,
                payment.PaymentMonth,
                payment.MonthlyLeaseAmount,
                payment.TDSApplicableId,
                payment.TDSRate,
                payment.TDSAmount,
                payment.NetPayableAmount,
                payment.LeaseEndDate,
                payment.PaymentStatus,
                payment.DSCApprovalStatus,
                payment.Remark,
                payment.ModifiedBy,
                ApprovalStatus = (int)payment.ApprovalStatus,
                payment.MakerUserId,
                payment.MakerUserName,
                MakerAction = (int)payment.MakerAction,
                payment.CheckerUserId,
                payment.CheckerUserName,
                payment.CheckerApprovalDate,
                payment.RejectionReason,
                payment.IsActiveRecord
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = CreateConnection();
            var sql = "DELETE FROM MonthlyRentPayments WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        #endregion

        #region Dropdown Data Methods

        public async Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync()
        {
            using var connection = CreateConnection();
            var sql = "SELECT Id, Name, Code FROM Employees WHERE IsActive = 1 ORDER BY Name";
            return await connection.QueryAsync<EmployeeName>(sql);
        }

        public async Task<IEnumerable<Owner>> GetOwnersAsync()
        {
            using var connection = CreateConnection();
            var sql = @"SELECT Id, VendorName as Name, VendorCode 
                       FROM Vendors 
                       WHERE IsActiveRecord = 1 AND ApprovalStatus = 2 
                       ORDER BY VendorName";
            return await connection.QueryAsync<Owner>(sql);
        }

        public async Task<IEnumerable<Owner>> GetOwnersByEmployeeAsync(int employeeId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT DISTINCT v.Id, v.VendorName as Name, v.VendorCode
                FROM Vendors v
                INNER JOIN Leases l ON l.VendorId = v.Id
                WHERE l.EmployeeId = @EmployeeId
                AND v.IsActiveRecord = 1 
                AND v.ApprovalStatus = 2
                ORDER BY v.VendorName";

            return await connection.QueryAsync<Owner>(sql, new { EmployeeId = employeeId });
        }

        public async Task<IEnumerable<Lease>> GetLeasesByEmployeeAndVendorAsync(int employeeId, int vendorId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT l.Id, l.RefNo, l.MonthlyRentPayable, l.EndDate
                FROM Leases l
                WHERE l.EmployeeId = @EmployeeId 
                AND l.VendorId = @VendorId
                AND l.IsActive = 1
                AND l.EndDate >= GETDATE()
                ORDER BY l.RefNo";

            return await connection.QueryAsync<Lease>(sql, new { EmployeeId = employeeId, VendorId = vendorId });
        }

        public async Task<Lease?> GetLeaseDetailsAsync(int leaseId)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT Id, RefNo, MonthlyRentPayable, EndDate
                FROM Leases
                WHERE Id = @LeaseId";

            return await connection.QueryFirstOrDefaultAsync<Lease>(sql, new { LeaseId = leaseId });
        }

        public async Task<IEnumerable<TdsApplicable>> GetTdsApplicableAsync()
        {
            using var connection = CreateConnection();
            var sql = "SELECT Id, Name, Rate FROM TDSApplicable WHERE IsActive = 1 ORDER BY Name";
            return await connection.QueryAsync<TdsApplicable>(sql);
        }

        public async Task<IEnumerable<LeaseName>> GetLeaseNameAsync()
        {
            using var connection = CreateConnection();
            var sql = "SELECT Id, RefNo FROM Leases WHERE IsActive = 1 ORDER BY RefNo";
            return await connection.QueryAsync<LeaseName>(sql);
        }

        public async Task ToggleActiveStatus(int? id)
        {
            using var connection = CreateConnection();
            var sql = @"
                UPDATE MonthlyRentPayments 
                SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END,
                    ModifiedDate = GETDATE()
                WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        #endregion

        #region Approval Workflow Methods

        public async Task<IEnumerable<MonthlyRentPayment>> GetApprovedPaymentsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT p.*, e.Name as EmployeeName, v.VendorName, l.RefNo as LeaseName
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY Id DESC, CheckerApprovalDate DESC, CreatedDate DESC) as RowNum
                    FROM MonthlyRentPayments 
                    WHERE ApprovalStatus = 2 AND IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                           OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%'))
                    AND (@StatusFilter IS NULL OR @StatusFilter = '' OR PaymentStatus = @StatusFilter)
                ) AS p
                LEFT JOIN Employees e ON p.EmployeeId = e.Id
                LEFT JOIN Vendors v ON p.VendorId = v.Id
                LEFT JOIN Leases l ON p.LeaseId = l.Id
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<MonthlyRentPayment>(sql, parameters);
        }

        public async Task<int> GetApprovedPaymentCountAsync(string searchTerm, string statusFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM MonthlyRentPayments p
                WHERE ApprovalStatus = 2 AND IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = p.EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                       OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = p.VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%'))
                AND (@StatusFilter IS NULL OR @StatusFilter = '' OR PaymentStatus = @StatusFilter)";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<IEnumerable<MonthlyRentPayment>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT p.*, e.Name as EmployeeName, v.VendorName, l.RefNo as LeaseName
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY Id DESC, CreatedDate DESC) as RowNum
                    FROM MonthlyRentPayments 
                    WHERE ApprovalStatus = 1 AND IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                           OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%')
                           OR MakerUserName LIKE '%' + @SearchTerm + '%')
                ) AS p
                LEFT JOIN Employees e ON p.EmployeeId = e.Id
                LEFT JOIN Vendors v ON p.VendorId = v.Id
                LEFT JOIN Leases l ON p.LeaseId = l.Id
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<MonthlyRentPayment>(sql, parameters);
        }

        public async Task<int> GetPendingApprovalCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM MonthlyRentPayments p
                WHERE ApprovalStatus = 1 AND IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = p.EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                       OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = p.VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%')
                       OR MakerUserName LIKE '%' + @SearchTerm + '%')";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<IEnumerable<MonthlyRentPayment>> GetRejectedPaymentsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT p.*, e.Name as EmployeeName, v.VendorName, l.RefNo as LeaseName
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY Id DESC, CheckerApprovalDate DESC) as RowNum
                    FROM MonthlyRentPayments 
                    WHERE ApprovalStatus = 3 AND IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                           OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%'))
                ) AS p
                LEFT JOIN Employees e ON p.EmployeeId = e.Id
                LEFT JOIN Vendors v ON p.VendorId = v.Id
                LEFT JOIN Leases l ON p.LeaseId = l.Id
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<MonthlyRentPayment>(sql, parameters);
        }

        public async Task<int> GetRejectedPaymentCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM MonthlyRentPayments p
                WHERE ApprovalStatus = 3 AND IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR EXISTS (SELECT 1 FROM Employees e WHERE e.Id = p.EmployeeId AND e.Name LIKE '%' + @SearchTerm + '%')
                       OR EXISTS (SELECT 1 FROM Vendors v WHERE v.Id = p.VendorId AND v.VendorName LIKE '%' + @SearchTerm + '%'))";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<bool> ApprovePaymentAsync(int id, string checkerUserId, string checkerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE MonthlyRentPayments 
                SET ApprovalStatus = 2, 
                    CheckerUserId = @CheckerUserId, 
                    CheckerUserName = @CheckerUserName, 
                    CheckerApprovalDate = GETDATE(),
                    ModifiedDate = GETDATE()
                WHERE Id = @Id AND ApprovalStatus = 1";

            var parameters = new
            {
                Id = id,
                CheckerUserId = checkerUserId,
                CheckerUserName = checkerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> RejectPaymentAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE MonthlyRentPayments 
                SET ApprovalStatus = 3, 
                    CheckerUserId = @CheckerUserId, 
                    CheckerUserName = @CheckerUserName, 
                    CheckerApprovalDate = GETDATE(),
                    RejectionReason = @RejectionReason,
                    ModifiedDate = GETDATE()
                WHERE Id = @Id AND ApprovalStatus = 1";

            var parameters = new
            {
                Id = id,
                CheckerUserId = checkerUserId,
                CheckerUserName = checkerUserName,
                RejectionReason = rejectionReason
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<int> AddPaymentForApprovalAsync(MonthlyRentPayment payment, string makerUserId, string makerUserName, MakerAction action)
        {
            payment.ApprovalStatus = ApprovalStatus.Pending;
            payment.MakerUserId = makerUserId;
            payment.MakerUserName = makerUserName;
            payment.MakerAction = action;
            payment.IsActiveRecord = true;
            payment.PaymentStatus = string.IsNullOrEmpty(payment.PaymentStatus) ? "Pending" : payment.PaymentStatus;
            payment.DSCApprovalStatus = string.IsNullOrEmpty(payment.DSCApprovalStatus) ? "Pending" : payment.DSCApprovalStatus;

            return await CreateAsync(payment);
        }

        public async Task<bool> UpdatePaymentForApprovalAsync(MonthlyRentPayment payment, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE MonthlyRentPayments 
                SET 
                    LeaseId = @LeaseId,
                    EmployeeId = @EmployeeId,
                    VendorId = @VendorId,
                    PaymentMonth = @PaymentMonth,
                    MonthlyLeaseAmount = @MonthlyLeaseAmount,
                    TDSApplicableId = @TDSApplicableId,
                    TDSRate = @TDSRate,
                    TDSAmount = @TDSAmount,
                    NetPayableAmount = @NetPayableAmount,
                    LeaseEndDate = @LeaseEndDate,
                    PaymentStatus = @PaymentStatus,
                    DSCApprovalStatus = @DSCApprovalStatus,
                    Remark = @Remark,
                    ApprovalStatus = 1, 
                    MakerUserId = @MakerUserId, 
                    MakerUserName = @MakerUserName, 
                    MakerAction = 2,
                    CheckerUserId = NULL,
                    CheckerUserName = NULL,
                    CheckerApprovalDate = NULL,
                    RejectionReason = NULL,
                    ModifiedDate = GETDATE()
                WHERE Id = @Id AND IsActiveRecord = 1";

            var parameters = new
            {
                payment.Id,
                payment.LeaseId,
                payment.EmployeeId,
                payment.VendorId,
                payment.PaymentMonth,
                payment.MonthlyLeaseAmount,
                payment.TDSApplicableId,
                payment.TDSRate,
                payment.TDSAmount,
                payment.NetPayableAmount,
                payment.LeaseEndDate,
                payment.PaymentStatus,
                payment.DSCApprovalStatus,
                payment.Remark,
                MakerUserId = makerUserId,
                MakerUserName = makerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> DeletePaymentForApprovalAsync(int id, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE MonthlyRentPayments 
                SET ApprovalStatus = 1, 
                    MakerUserId = @MakerUserId, 
                    MakerUserName = @MakerUserName, 
                    MakerAction = 3,
                    CheckerUserId = NULL,
                    CheckerUserName = NULL,
                    CheckerApprovalDate = NULL,
                    RejectionReason = NULL,
                    ModifiedDate = GETDATE()
                WHERE Id = @Id";

            var parameters = new
            {
                Id = id,
                MakerUserId = makerUserId,
                MakerUserName = makerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> HasPendingChangesAsync(int id)
        {
            using var connection = CreateConnection();
            var sql = "SELECT COUNT(*) FROM MonthlyRentPayments WHERE Id = @Id AND ApprovalStatus = 1";
            var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
            return count > 0;
        }

        #endregion
    }
}