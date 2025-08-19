using Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;
using RentManagement.Models;
using System.Data;
using System.Data.SqlClient;

namespace RentManagement.Data
{
    public class MISReportRepository : IMISReportRepository
    {
        private readonly string _connectionString;

        public MISReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);

        public async Task<IEnumerable<EmployeeReport>> GetEmployeeReportAsync(EmployeeReportFilter filter)
        {
            using var conn = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@FinancialYear", filter.FinancialYear);
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);
            parameters.Add("@EmployeeName", filter.EmployeeName);
            parameters.Add("@Department", filter.Department);

            return await conn.QueryAsync<EmployeeReport>(
                "sp_GetEmployeeReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<LeaseReport>> GetLeaseReportAsync(LeaseReportFilter filter)
        {
            using var conn = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@FinancialYear", filter.FinancialYear);
            parameters.Add("@LeaseStartDate", filter.LeaseStartDate);
            parameters.Add("@LeaseEndDate", filter.LeaseEndDate);
            parameters.Add("@EmployeeName", filter.EmployeeName);
            parameters.Add("@VendorName", filter.VendorName);

            return await conn.QueryAsync<LeaseReport>(
                "sp_GetLeaseReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<EmployeePaymentReport>> GetEmployeePaymentReportAsync(EmployeePaymentReportFilter filter)
        {
            using var conn = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeName", filter.EmployeeName);
            parameters.Add("@PaymentType", filter.PaymentType);
            parameters.Add("@FromDate", filter.FromDate);
            parameters.Add("@ToDate", filter.ToDate);

            return await conn.QueryAsync<EmployeePaymentReport>(
                "sp_GetEmployeePaymentReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
       }
}
