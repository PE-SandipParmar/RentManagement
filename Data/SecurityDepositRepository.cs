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
        parameters.Add("@ApprovalStatus", deposit.ApprovalStatus);
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
        parameters.Add("@ApprovalStatus", deposit.ApprovalStatus);
        parameters.Add("@ModifiedBy", deposit.ModifiedBy);
        parameters.Add("@IsActive", deposit.IsActive);

        var affectedRows = await connection.ExecuteAsync(
            "SecurityDepositUpdate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        var affectedRows = await connection.ExecuteAsync(
            "SecurityDepositDelete",
            new { Id = id },
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
}
