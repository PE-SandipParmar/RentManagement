using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using RentManagement.Models;
using RentManagement.Data;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Data.SqlClient;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;

public class MonthlyRentPaymentRepository : IMonthlyRentPaymentRepository
{
    private readonly string _connectionString;

    public MonthlyRentPaymentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);

    public async Task<int> CreateAsync(MonthlyRentPayment payment)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@LeaseId", payment.LeaseId);
        parameters.Add("@EmployeeId", payment.EmployeeId);
        parameters.Add("@VendorId", payment.VendorId);
        parameters.Add("@PaymentMonth", payment.PaymentMonth);
        parameters.Add("@MonthlyLeaseAmount", payment.MonthlyLeaseAmount);
        parameters.Add("@TDSApplicableId", payment.TDSApplicableId);
        parameters.Add("@TDSRate", payment.TDSRate);
        parameters.Add("@TDSAmount", payment.TDSAmount);
        parameters.Add("@NetPayableAmount", payment.NetPayableAmount);
        parameters.Add("@PaymentStatus", payment.PaymentStatus);
        parameters.Add("@DSCApprovalStatus", payment.DSCApprovalStatus);
        parameters.Add("@ApprovedBy", payment.ApprovedBy);
        parameters.Add("@ApprovedDate", payment.ApprovedDate);
        parameters.Add("@PaymentDate", payment.PaymentDate);
        parameters.Add("@TransactionReference", payment.TransactionReference);
        parameters.Add("@Remark", payment.Remark);
        parameters.Add("@CreatedBy", payment.CreatedBy);
        parameters.Add("@IsActive", payment.IsActive);

        parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "MonthlyRentPaymentCreate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<int>("@Id");
    }

    public async Task<MonthlyRentPayment?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        var payment = await connection.QuerySingleOrDefaultAsync<MonthlyRentPayment>(
            "MonthlyRentPaymentGetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
        return payment;
    }

    public async Task<PagedResult<MonthlyRentPayment>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Page", page);
        parameters.Add("@PageSize", pageSize);
        parameters.Add("@Search", search ?? string.Empty);

        using var multi = await connection.QueryMultipleAsync(
            "MonthlyRentPaymentRead", 
            parameters,
            commandType: CommandType.StoredProcedure);

        var Payments = (await multi.ReadAsync<MonthlyRentPayment>()).ToList();
        var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

        return new PagedResult<MonthlyRentPayment>
        {
            Items = Payments,
            TotalItems = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateAsync(MonthlyRentPayment payment)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Id", payment.Id);
        parameters.Add("@LeaseId", payment.LeaseId);
        parameters.Add("@EmployeeId", payment.EmployeeId);
        parameters.Add("@VendorId", payment.VendorId);
        parameters.Add("@PaymentMonth", payment.PaymentMonth);
        parameters.Add("@MonthlyLeaseAmount", payment.MonthlyLeaseAmount);
        parameters.Add("@TDSApplicableId", payment.TDSApplicableId);
        parameters.Add("@TDSRate", payment.TDSRate);
        parameters.Add("@TDSAmount", payment.TDSAmount);
        parameters.Add("@NetPayableAmount", payment.NetPayableAmount);
        parameters.Add("@PaymentStatus", payment.PaymentStatus);
        parameters.Add("@ApprovedBy", payment.ApprovedBy);
        parameters.Add("@ApprovedDate", payment.ApprovedDate);
        parameters.Add("@PaymentDate", payment.PaymentDate);
        parameters.Add("@TransactionReference", payment.TransactionReference);
        parameters.Add("@Remark", payment.Remark);
        parameters.Add("@ModifiedBy", payment.ModifiedBy);
        parameters.Add("@IsActive", payment.IsActive);

        var affectedRows = await connection.ExecuteAsync(
            "MonthlyRentPaymentUpdate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        var affectedRows = await connection.ExecuteAsync(
            "MonthlyRentPaymentsDelete",
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
    }public async Task<IEnumerable<Owner>> GetOwnersByEmployeeAsync(int employeeid)
    {
        using var connection = CreateConnection();
        var query = @"
                SELECT 
                    l.name
                FROM employees l
                WHERE l.id = @EmployeeId"
      ;

        var dbemployeeName = await connection.QuerySingleAsync<string>(query, new { EmployeeId = employeeid });



        var parameters = new DynamicParameters();
        parameters.Add("@Name", dbemployeeName);

        return await connection.QueryAsync<Owner>(
            "[VendorReadbyEmployee]",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<Lease>> GetLeasesByEmployeeAndVendorAsync(int employeeId, int vendorId)
    {
        using var connection = new SqlConnection(_connectionString);

        //var query = @"
        //        SELECT 
        //            l.Id
        //        FROM employees l
        //        WHERE l.name = @EmployeeId"
        //;

        //var dbemployeeid = await connection.QuerySingleAsync<int>(query, new { EmployeeId = employeeId });

        var parameters = new DynamicParameters();
        parameters.Add("@EmployeeId", employeeId);
        parameters.Add("@VendorId", vendorId);

        return await connection.QueryAsync<Lease>(
                   "[LeaseGetByEmployeeandVendorId]",
                   parameters,
                   commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<TdsApplicable>> GetTdsApplicableAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<TdsApplicable>(
            "TDSApplicableRead",
            commandType: CommandType.StoredProcedure);
    }
    public async Task<IEnumerable<LeaseName>> GetLeaseNameAsync()
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<LeaseName>(
            "LeaseNamesRead",
            commandType: CommandType.StoredProcedure);
    }
    public async Task ToggleActiveStatus(int? Id)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "ToggleMonthlyRentPaymentsActive",
            new { Id = Id },
            commandType: CommandType.StoredProcedure
        );
    }

}
