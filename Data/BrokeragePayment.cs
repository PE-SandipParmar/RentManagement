using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using RentManagement.Models;
using RentManagement.Data;
using DocumentFormat.OpenXml.Office2010.Excel;
using System.Data.SqlClient;
using DocumentFormat.OpenXml.Wordprocessing;

public class BrokeragePaymentRepository : IBrokeragePaymentRepository
{
    private readonly string _connectionString;

    public BrokeragePaymentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);

    public async Task<int> CreateAsync(BrokeragePayment payment)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@LeaseId", payment.LeaseId);
        parameters.Add("@EmployeeId", payment.EmployeeId);
        parameters.Add("@VendorId", payment.VendorId);
        parameters.Add("@PaymentMonth", payment.PaymentMonth);
        parameters.Add("@BrokerageAmount", payment.BrokerageAmount);
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

        // Assuming stored proc BrokeragePayment_Create returns inserted Id as output param
        parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "BrokeragePaymentCreate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<int>("@Id");
    }

    public async Task<BrokeragePayment?> GetByIdAsync(int id)
    {
        using var connection = CreateConnection();

        var payment = await connection.QuerySingleOrDefaultAsync<BrokeragePayment>(
            "BrokeragePaymentGetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
        return payment;
    }

    public async Task<PagedResult<BrokeragePayment>> GetAllAsync(int page, int pageSize, string? search)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Page", page);
        parameters.Add("@PageSize", pageSize);
        parameters.Add("@Search", search ?? string.Empty);

        using var multi = await connection.QueryMultipleAsync(
            "BrokeragePaymentRead", 
            parameters,
            commandType: CommandType.StoredProcedure);

        var Payments = (await multi.ReadAsync<BrokeragePayment>()).ToList();
        var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

        return new PagedResult<BrokeragePayment>
        {
            Items = Payments,
            TotalItems = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateAsync(BrokeragePayment payment)
    {
        using var connection = CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Id", payment.Id);
        parameters.Add("@LeaseId", payment.LeaseId);
        parameters.Add("@EmployeeId", payment.EmployeeId);
        parameters.Add("@VendorId", payment.VendorId);
        parameters.Add("@PaymentMonth", payment.PaymentMonth);
        parameters.Add("@BrokerageAmount", payment.BrokerageAmount);
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
            "BrokeragePaymentUpdate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = CreateConnection();

        var affectedRows = await connection.ExecuteAsync(
            "BrokeragePaymentsDelete",
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
}
