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

    public async Task<int> CreateAsync(SecurityDeposit deposit)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@EmployeeId", deposit.EmployeeId);
        parameters.Add("@VendorId", deposit.VendorId);
        parameters.Add("@LeaseId", deposit.LeaseId);
        parameters.Add("@Amount", deposit.Amount);
        parameters.Add("@TdsRate", deposit.TdsRate);
        parameters.Add("@TdsAmount", deposit.TdsAmount);
        parameters.Add("@ApprovalStatus", deposit.ApprovalStatus);
        parameters.Add("@Remark", deposit.Remark);
        parameters.Add("@CreatedBy", deposit.CreatedBy);
        parameters.Add("@IsActive", deposit.IsActive);

        parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "SecurityDepositCreate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<int>("@Id");
    }

    public async Task<SecurityDeposit?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<SecurityDeposit>(
            "SecurityDepositGetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<PagedResult<SecurityDeposit>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Page", page);
        parameters.Add("@PageSize", pageSize);
        parameters.Add("@Search", search ?? string.Empty);

        using var multi = await connection.QueryMultipleAsync(
            "SecurityDepositRead",
            parameters,
            commandType: CommandType.StoredProcedure);

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

        var parameters = new DynamicParameters();
        parameters.Add("@Id", deposit.Id);
        parameters.Add("@EmployeeId", deposit.EmployeeId);
        parameters.Add("@VendorId", deposit.VendorId);
        parameters.Add("@LeaseId", deposit.LeaseId);
        parameters.Add("@Amount", deposit.Amount);
        parameters.Add("@TdsRate", deposit.TdsRate);
        parameters.Add("@TdsAmount", deposit.TdsAmount);
        parameters.Add("@Remark", deposit.Remark);
        parameters.Add("@ApprovalStatus", deposit.ApprovalStatus);
        parameters.Add("@ModifiedBy", deposit.ModifiedBy);
        parameters.Add("@IsActive", deposit.IsActive);

        var affectedRows = await connection.ExecuteAsync(
            "SecurityDepositUpdate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id,int ModifiedBy)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);
        parameters.Add("@ModifiedBy", id);

        var affectedRows = await connection.ExecuteAsync(
           "SecurityDepositDelete",
           parameters,
           commandType: CommandType.StoredProcedure);


        
        return affectedRows > 0;
    }

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
            "ToggleSecurityDepositActive",
            new { Id = Id },
            commandType: CommandType.StoredProcedure
        );
    }

    #region Enhanced Functionality Methods

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

            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeId", employeeId);
            parameters.Add("@LeaseId", leaseId);
            parameters.Add("@VendorId", vendorId);
            parameters.Add("@ExcludeId", excludeId);
            parameters.Add("@IsDuplicate", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "CheckDuplicateSecurityDeposit",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<bool>("@IsDuplicate");
        }
        catch (Exception)
        {
            return false; // In case of error, assume no duplicate
        }
    }

    #endregion
}