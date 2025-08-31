using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using RentManagement.Models;
using System.Data.SqlClient;
using RentManagement.Data;

public class SecurityDepositRepository : ISecurityDepositRepository
{
    private readonly string _connectionString;

    public SecurityDepositRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);

    #region Existing Methods (Updated for Approval Workflow)

    public async Task<int> CreateAsync(SecurityDeposit deposit)
    {
        using var connection = CreateConnection();

        var sql = @"
            INSERT INTO SecurityDeposit (
                EmployeeId, VendorId, LeaseId, Amount, TdsRate, TdsAmount,
                Remark, ApprovalStatus, MakerUserId, MakerUserName,
                MakerAction, CheckerUserId, CheckerUserName, ApprovalDate,
                RejectionReason, IsActiveRecord, IsActive, CreatedBy, CreatedDate
            )
            VALUES (
                @EmployeeId, @VendorId, @LeaseId, @Amount, @TdsRate, @TdsAmount,
                @Remark, @ApprovalStatus, @MakerUserId, @MakerUserName,
                @MakerAction, @CheckerUserId, @CheckerUserName, @ApprovalDate,
                @RejectionReason, @IsActiveRecord, @IsActive, @CreatedBy, GETDATE()
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var parameters = new
        {
            EmployeeId = deposit.EmployeeId,
            VendorId = deposit.VendorId,
            LeaseId = deposit.LeaseId,
            Amount = deposit.Amount,
            TdsRate = deposit.TdsRate,
            TdsAmount = deposit.TdsAmount,
            Remark = deposit.Remark,
            ApprovalStatus = (int)deposit.ApprovalStatus,
            MakerUserId = deposit.MakerUserId,
            MakerUserName = deposit.MakerUserName,
            MakerAction = (int)deposit.MakerAction,
            CheckerUserId = deposit.CheckerUserId,
            CheckerUserName = deposit.CheckerUserName,
            ApprovalDate = deposit.ApprovalDate,
            RejectionReason = deposit.RejectionReason,
            IsActiveRecord = deposit.IsActiveRecord,
            IsActive = deposit.IsActive,
            CreatedBy = deposit.CreatedBy
        };

        var result = await connection.QuerySingleAsync<int>(sql, parameters);
        return result;
    }

    public async Task<SecurityDeposit?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        var sql = @"
            SELECT sd.*, 
                   e.Name as EmployeeName,
                   v.VendorName as VendorName,
                   l.RefNo as LeaseName,
                   CASE 
                       WHEN sd.ApprovalStatus = 1 THEN 'Pending'
                       WHEN sd.ApprovalStatus = 2 THEN 'Approved'
                       WHEN sd.ApprovalStatus = 3 THEN 'Rejected'
                       ELSE 'Unknown'
                   END as ApprovalStatusText
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.Id = @Id AND sd.IsActiveRecord = 1";

        return await connection.QuerySingleOrDefaultAsync<SecurityDeposit>(sql, new { Id = id });
    }

    public async Task<PagedResult<SecurityDeposit>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var connection = CreateConnection();

        var offset = (page - 1) * pageSize;

        var sql = @"
            SELECT sd.*, 
                   e.Name as EmployeeName,
                   v.VendorName as VendorName,
                   l.RefNo as LeaseName,
                   CASE 
                       WHEN sd.ApprovalStatus = 1 THEN 'Pending'
                       WHEN sd.ApprovalStatus = 2 THEN 'Approved'
                       WHEN sd.ApprovalStatus = 3 THEN 'Rejected'
                       ELSE 'Unknown'
                   END as ApprovalStatusText
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.IsActiveRecord = 1
            AND (@Search = '' OR e.Name LIKE '%' + @Search + '%' OR v.VendorName LIKE '%' + @Search + '%' OR l.Name LIKE '%' + @Search + '%')
            ORDER BY sd.CreatedDate DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*)
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.IsActiveRecord = 1
            AND (@Search = '' OR e.Name LIKE '%' + @Search + '%' OR v.VendorName LIKE '%' + @Search + '%' OR l.Name LIKE '%' + @Search + '%')";

        using var multi = await connection.QueryMultipleAsync(sql, new { Search = search ?? "", Offset = offset, PageSize = pageSize });

        var deposits = (await multi.ReadAsync<SecurityDeposit>()).ToList();
        var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

        return new PagedResult<SecurityDeposit>
        {
            Items = deposits,
            TotalItems = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateAsync(SecurityDeposit deposit)
    {
        using var connection = CreateConnection();

        var sql = @"
            UPDATE SecurityDeposit
            SET 
                EmployeeId = @EmployeeId,
                VendorId = @VendorId,
                LeaseId = @LeaseId,
                Amount = @Amount,
                TdsRate = @TdsRate,
                TdsAmount = @TdsAmount,
                Remark = @Remark,
                ApprovalStatus = @ApprovalStatus,
                MakerUserId = @MakerUserId,
                MakerUserName = @MakerUserName,
                CheckerUserId = @CheckerUserId,
                CheckerUserName = @CheckerUserName,
                MakerAction = @MakerAction,
                ApprovalDate = @ApprovalDate,
                RejectionReason = @RejectionReason,
                IsActiveRecord = @IsActiveRecord,
                IsActive = @IsActive,
                ModifiedBy = @ModifiedBy,
                ModifiedDate = GETDATE()
            WHERE Id = @Id";

        var parameters = new
        {
            Id = deposit.Id,
            EmployeeId = deposit.EmployeeId,
            VendorId = deposit.VendorId,
            LeaseId = deposit.LeaseId,
            Amount = deposit.Amount,
            TdsRate = deposit.TdsRate,
            TdsAmount = deposit.TdsAmount,
            Remark = deposit.Remark,
            ApprovalStatus = (int)deposit.ApprovalStatus,
            MakerUserId = deposit.MakerUserId,
            MakerUserName = deposit.MakerUserName,
            CheckerUserId = deposit.CheckerUserId,
            CheckerUserName = deposit.CheckerUserName,
            MakerAction = (int)deposit.MakerAction,
            ApprovalDate = deposit.ApprovalDate,
            RejectionReason = deposit.RejectionReason,
            IsActiveRecord = deposit.IsActiveRecord,
            IsActive = deposit.IsActive,
            ModifiedBy = deposit.ModifiedBy
        };

        var affectedRows = await connection.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, int ModifiedBy)
    {
        using var connection = CreateConnection();

        var sql = @"
            UPDATE SecurityDeposit 
            SET IsActiveRecord = 0, 
                ModifiedBy = @ModifiedBy,
                ModifiedDate = GETDATE()
            WHERE Id = @Id";

        var affectedRows = await connection.ExecuteAsync(sql, new { Id = id, ModifiedBy = ModifiedBy });
        return affectedRows > 0;
    }

    #endregion

    #region Approval Workflow Methods

    public async Task<IEnumerable<SecurityDeposit>> GetAllSecurityDepositsWithApprovalStatusAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
    {
        using var connection = CreateConnection();

        var offset = (pageNumber - 1) * pageSize;

        var sql = @"
            SELECT sd.*, 
                   e.Name as EmployeeName,
                   v.VendorName as VendorName,
                   l.RefNo as LeaseName,
                   CASE 
                       WHEN sd.ApprovalStatus = 1 THEN 'Pending'
                       WHEN sd.ApprovalStatus = 2 THEN 'Approved'
                       WHEN sd.ApprovalStatus = 3 THEN 'Rejected'
                       ELSE 'Unknown'
                   END as ApprovalStatusText
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.IsActiveRecord = 1
            AND (@SearchTerm = '' OR e.Name LIKE '%' + @SearchTerm + '%' OR v.VendorName LIKE '%' + @SearchTerm + '%')
            AND (@StatusFilter = '' OR (sd.IsActive = 1 AND @StatusFilter = 'Active') OR (sd.IsActive = 0 AND @StatusFilter = 'Inactive'))
            ORDER BY sd.CreatedDate DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var parameters = new
        {
            SearchTerm = searchTerm ?? "",
            StatusFilter = statusFilter ?? "",
            Offset = offset,
            PageSize = pageSize
        };

        return await connection.QueryAsync<SecurityDeposit>(sql, parameters);
    }

    public async Task<int> GetAllSecurityDepositsWithApprovalStatusCountAsync(string searchTerm, string statusFilter)
    {
        using var connection = CreateConnection();

        var sql = @"
            SELECT COUNT(*)
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            WHERE sd.IsActiveRecord = 1
            AND (@SearchTerm = '' OR e.Name LIKE '%' + @SearchTerm + '%' OR v.VendorName LIKE '%' + @SearchTerm + '%')
            AND (@StatusFilter = '' OR (sd.IsActive = 1 AND @StatusFilter = 'Active') OR (sd.IsActive = 0 AND @StatusFilter = 'Inactive'))";

        var parameters = new
        {
            SearchTerm = searchTerm ?? "",
            StatusFilter = statusFilter ?? ""
        };

        return await connection.QuerySingleAsync<int>(sql, parameters);
    }

    public async Task<IEnumerable<SecurityDeposit>> GetApprovedSecurityDepositsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
    {
        using var connection = CreateConnection();

        var offset = (pageNumber - 1) * pageSize;

        var sql = @"
            SELECT sd.*, 
                   e.Name as EmployeeName,
                   v.VendorName as VendorName,
                   l.RefNo as LeaseName
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.ApprovalStatus = 2 AND sd.IsActiveRecord = 1
            AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                   OR e.Name LIKE '%' + @SearchTerm + '%' 
                   OR v.VendorName LIKE '%' + @SearchTerm + '%')
            AND (@StatusFilter IS NULL OR @StatusFilter = '' 
                   OR (sd.IsActive = 1 AND @StatusFilter = 'Active') 
                   OR (sd.IsActive = 0 AND @StatusFilter = 'Inactive'))
            ORDER BY sd.ApprovalDate DESC, sd.CreatedDate DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var parameters = new
        {
            SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
            StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
            Offset = offset,
            PageSize = pageSize
        };

        return await connection.QueryAsync<SecurityDeposit>(sql, parameters);
    }

    public async Task<int> GetApprovedSecurityDepositCountAsync(string searchTerm, string statusFilter)
    {
        using var connection = CreateConnection();

        var sql = @"
            SELECT COUNT(*)
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            WHERE sd.ApprovalStatus = 2 AND sd.IsActiveRecord = 1
            AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                   OR e.Name LIKE '%' + @SearchTerm + '%' 
                   OR v.VendorName LIKE '%' + @SearchTerm + '%')
            AND (@StatusFilter IS NULL OR @StatusFilter = '' 
                   OR (sd.IsActive = 1 AND @StatusFilter = 'Active') 
                   OR (sd.IsActive = 0 AND @StatusFilter = 'Inactive'))";

        var parameters = new
        {
            SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
            StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter
        };

        return await connection.QuerySingleAsync<int>(sql, parameters);
    }

    public async Task<IEnumerable<SecurityDeposit>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize)
    {
        using var connection = CreateConnection();

        var offset = (pageNumber - 1) * pageSize;

        var sql = @"
            SELECT sd.*, 
                   e.Name as EmployeeName,
                   v.VendorName as VendorName,
                   l.RefNo as LeaseName
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.ApprovalStatus = 1 AND sd.IsActiveRecord = 1
            AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                   OR e.Name LIKE '%' + @SearchTerm + '%' 
                   OR v.VendorName LIKE '%' + @SearchTerm + '%'
                   OR sd.MakerUserName LIKE '%' + @SearchTerm + '%')
            ORDER BY sd.CreatedDate DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var parameters = new
        {
            SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
            Offset = offset,
            PageSize = pageSize
        };

        return await connection.QueryAsync<SecurityDeposit>(sql, parameters);
    }

    public async Task<int> GetPendingApprovalCountAsync(string searchTerm)
    {
        using var connection = CreateConnection();

        var sql = @"
            SELECT COUNT(*)
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            WHERE sd.ApprovalStatus = 1 AND sd.IsActiveRecord = 1
            AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                   OR e.Name LIKE '%' + @SearchTerm + '%' 
                   OR v.VendorName LIKE '%' + @SearchTerm + '%'
                   OR sd.MakerUserName LIKE '%' + @SearchTerm + '%')";

        var parameters = new
        {
            SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
        };

        return await connection.QuerySingleAsync<int>(sql, parameters);
    }

    public async Task<IEnumerable<SecurityDeposit>> GetRejectedSecurityDepositsAsync(string searchTerm, int pageNumber, int pageSize)
    {
        using var connection = CreateConnection();

        var offset = (pageNumber - 1) * pageSize;

        var sql = @"
            SELECT sd.*, 
                   e.Name as EmployeeName,
                   v.VendorName as VendorName,
                   l.RefNo as LeaseName
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            LEFT JOIN Leases l ON sd.LeaseId = l.Id
            WHERE sd.ApprovalStatus = 3 AND sd.IsActiveRecord = 1
            AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                   OR e.Name LIKE '%' + @SearchTerm + '%' 
                   OR v.VendorName LIKE '%' + @SearchTerm + '%')
            ORDER BY sd.ApprovalDate DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var parameters = new
        {
            SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
            Offset = offset,
            PageSize = pageSize
        };

        return await connection.QueryAsync<SecurityDeposit>(sql, parameters);
    }

    public async Task<int> GetRejectedSecurityDepositCountAsync(string searchTerm)
    {
        using var connection = CreateConnection();

        var sql = @"
            SELECT COUNT(*)
            FROM SecurityDeposit sd
            LEFT JOIN Employees e ON sd.EmployeeId = e.Id
            LEFT JOIN Vendors v ON sd.VendorId = v.Id
            WHERE sd.ApprovalStatus = 3 AND sd.IsActiveRecord = 1
            AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                   OR e.Name LIKE '%' + @SearchTerm + '%' 
                   OR v.VendorName LIKE '%' + @SearchTerm + '%')";

        var parameters = new
        {
            SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
        };

        return await connection.QuerySingleAsync<int>(sql, parameters);
    }

    public async Task<bool> ApproveSecurityDepositAsync(int id, string checkerUserId, string checkerUserName)
    {
        using var connection = CreateConnection();

        var sql = @"
            UPDATE SecurityDeposit 
            SET ApprovalStatus = 2, 
                CheckerUserId = @CheckerUserId, 
                CheckerUserName = @CheckerUserName, 
                ApprovalDate = GETDATE(),
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

    public async Task<bool> RejectSecurityDepositAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason)
    {
        using var connection = CreateConnection();

        var sql = @"
            UPDATE SecurityDeposit 
            SET ApprovalStatus = 3, 
                CheckerUserId = @CheckerUserId, 
                CheckerUserName = @CheckerUserName, 
                ApprovalDate = GETDATE(),
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

    public async Task<int> AddSecurityDepositForApprovalAsync(SecurityDeposit deposit, string makerUserId, string makerUserName, MakerAction action)
    {
        deposit.ApprovalStatus = ApprovalStatus.Pending;
        deposit.MakerUserId = makerUserId;
        deposit.MakerUserName = makerUserName;
        deposit.MakerAction = action;
        deposit.IsActiveRecord = true;
        deposit.CreatedBy = makerUserName;

        return await CreateAsync(deposit);
    }

    public async Task<bool> UpdateSecurityDepositForApprovalAsync(SecurityDeposit deposit, string makerUserId, string makerUserName)
    {
        using var connection = CreateConnection();

        var sql = @"
            UPDATE SecurityDeposit 
            SET 
                EmployeeId = @EmployeeId,
                VendorId = @VendorId,
                LeaseId = @LeaseId,
                Amount = @Amount,
                TdsRate = @TdsRate,
                TdsAmount = @TdsAmount,
                Remark = @Remark,
                ApprovalStatus = 1, 
                MakerUserId = @MakerUserId, 
                MakerUserName = @MakerUserName, 
                MakerAction = 2,
                CheckerUserId = NULL,
                CheckerUserName = NULL,
                ApprovalDate = NULL,
                RejectionReason = NULL,
                ModifiedDate = GETDATE()
            WHERE Id = @Id AND IsActiveRecord = 1";

        var parameters = new
        {
            Id = deposit.Id,
            EmployeeId = deposit.EmployeeId,
            VendorId = deposit.VendorId,
            LeaseId = deposit.LeaseId,
            Amount = deposit.Amount,
            TdsRate = deposit.TdsRate,
            TdsAmount = deposit.TdsAmount,
            Remark = deposit.Remark,
            MakerUserId = makerUserId,
            MakerUserName = makerUserName
        };

        var affectedRows = await connection.ExecuteAsync(sql, parameters);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteSecurityDepositForApprovalAsync(int id, string makerUserId, string makerUserName)
    {
        using var connection = CreateConnection();

        var sql = @"
            UPDATE SecurityDeposit 
            SET ApprovalStatus = 1, 
                MakerUserId = @MakerUserId, 
                MakerUserName = @MakerUserName, 
                MakerAction = 3,
                CheckerUserId = NULL,
                CheckerUserName = NULL,
                ApprovalDate = NULL,
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

    public async Task<SecurityDeposit?> GetOriginalSecurityDepositForUpdateAsync(int id)
    {
        using var connection = CreateConnection();
        var parameters = new { Id = id };
        return await connection.QueryFirstOrDefaultAsync<SecurityDeposit>(
            "SELECT * FROM SecurityDeposit WHERE Id = @Id AND ApprovalStatus = 2",
            parameters);
    }

    public async Task<bool> HasPendingChangesAsync(int id)
    {
        using var connection = CreateConnection();
        var parameters = new { Id = id };
        var count = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM SecurityDeposit WHERE Id = @Id AND ApprovalStatus = 1",
            parameters);
        return count > 0;
    }

    #endregion

    #region Other Methods

    public async Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<EmployeeName>(
            "EmployeeNamesRead",
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<Owner>> GetOwnersAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<Owner>(
            "VendorRead",
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<LeaseName>> GetLeaseNamesAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<LeaseName>(
            "LeaseNamesRead",
            commandType: CommandType.StoredProcedure);
    }

    public async Task ToggleActiveStatus(int Id)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE SecurityDeposit SET IsActive = ~IsActive WHERE Id = @Id",
            new { Id = Id }
        );
    }

    public async Task<decimal> GetEmployeeSalaryAsync(int employeeId)
    {
        try
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeId", employeeId);
            parameters.Add("@Salary", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "GetEmployeeSalary",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<decimal>("@Salary");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting employee salary: {ex.Message}", ex);
        }
    }

    public async Task<List<dynamic>> GetLeasesByEmployeeAsync(int employeeId)
    {
        try
        {
            using var connection = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeId", employeeId);

            var leases = await connection.QueryAsync(
                "GetLeasesByEmployee",
                parameters,
                commandType: CommandType.StoredProcedure);

            return leases.ToList();
        }
        catch (Exception)
        {
            return new List<dynamic>();
        }
    }

    public async Task<int> GetLeaseOwnerAsync(int leaseId)
    {
        try
        {
            using var connection = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@LeaseId", leaseId);
            parameters.Add("@OwnerId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "GetLeaseOwner",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@OwnerId");
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<bool> IsDuplicateRecordAsync(int employeeId, int leaseId, int vendorId, int? excludeId = null)
    {
        try
        {
            using var connection = CreateConnection();
            var sql = @"
                SELECT COUNT(*) 
                FROM SecurityDeposit
                WHERE EmployeeId = @EmployeeId 
                AND LeaseId = @LeaseId 
                AND VendorId = @VendorId 
                AND IsActiveRecord = 1
                AND (@ExcludeId IS NULL OR Id != @ExcludeId)";

            var count = await connection.QuerySingleAsync<int>(sql, new
            {
                EmployeeId = employeeId,
                LeaseId = leaseId,
                VendorId = vendorId,
                ExcludeId = excludeId
            });

            return count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion
}