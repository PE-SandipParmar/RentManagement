using System.Data;
using System.Data.SqlClient;
using Dapper;

using Microsoft.Extensions.Configuration;
using RentManagement.Data;
using RentManagement.Models.RentPaymentSystem.Models;
using RentManagement.Models;

namespace RentPaymentSystem.Repositories
{
    public class MISReportRepository : IMISReportRepository
    {
        private readonly string _connectionString;

        public MISReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<EmployeeReports>> GetEmployeeReportAsync(ReportRequestDto request)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", request.FromDate);
            parameters.Add("@ToDate", request.ToDate);
            parameters.Add("@ActiveOnly", request.ActiveOnly);
            parameters.Add("@Department", request.Department);

            var employees = await connection.QueryAsync<EmployeeReports>(
                "SP_GetEmployeeReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return employees.ToList();
        }

        public async Task<List<LeaseReports>> GetLeaseReportAsync(ReportRequestDto request)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", request.FromDate);
            parameters.Add("@ToDate", request.ToDate);
            parameters.Add("@Status", request.Status);
            parameters.Add("@ActiveOnly", request.ActiveOnly);

            var query = @"
                SELECT l.*, e.Name as EmployeeName, e.Code as EmployeeCode, 
                       v.VendorName, v.VendorCode
                FROM Leases l
                INNER JOIN Employees e ON l.EmployeeId = e.Id
                INNER JOIN Vendors v ON l.VendorId = v.Id";

            var leases = await connection.QueryAsync<dynamic>(
                "SP_GetLeaseReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return leases.Select(l => new LeaseReports
            {
                Id = l.Id,
                RefNo = l.RefNo,
                EmployeeId = l.EmployeeId,
                VendorId = l.VendorId,
                MonthlyRent = l.MonthlyRent,
                SecurityDeposit = l.SecurityDeposit,
                Status = l.Status,
                FromDate = l.FromDate,
                EndDate = l.EndDate,
                CreatedDate = l.CreatedDate,
                ModifiedDate = l.ModifiedDate,
                Employee = new EmployeeReports
                {
                    Name = l.EmployeeName,
                    Code = l.EmployeeCode
                },
                Vendor = new VendorReports
                {
                    VendorName = l.VendorName,
                    VendorCode = l.VendorCode
                }
            }).ToList();
        }

        public async Task<List<VendorReports>> GetVendorReportAsync(ReportRequestDto request)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@ActiveOnly", request.ActiveOnly);
            parameters.Add("@IncludeFinancials", request.IncludeFinancials);

            var vendors = await connection.QueryAsync<VendorReports>(
                "SP_GetVendorReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return vendors.ToList();
        }

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(ReportRequestDto request)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", request.FromDate);
            parameters.Add("@ToDate", request.ToDate);

            using var multi = await connection.QueryMultipleAsync(
                "SP_GetFinancialSummary",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var summary = await multi.ReadFirstAsync<FinancialSummaryDto>();
            var trends = await multi.ReadAsync<MonthlyRentTrend>();
            var departmentExpenses = await multi.ReadAsync<DepartmentWiseExpense>();

            summary.RentTrends = trends.ToList();
            summary.DepartmentExpenses = departmentExpenses.ToList();

            return summary;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var stats = await connection.QueryFirstAsync<DashboardStatsDto>(
                "SP_GetDashboardStats",
                commandType: CommandType.StoredProcedure
            );

            return stats;
        }

        public async Task<List<MonthlyRentTrend>> GetRentTrendsAsync(int months = 6)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@Months", months);

            var trends = await connection.QueryAsync<MonthlyRentTrend>(
                "SP_GetRentTrends",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return trends.ToList();
        }

        public async Task<bool> SaveReportLogAsync(string reportType, string fileName, string status)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@ReportType", reportType);
            parameters.Add("@FileName", fileName);
            parameters.Add("@Status", status);
            parameters.Add("@GeneratedDate", DateTime.Now);

            var result = await connection.ExecuteAsync(
                "SP_SaveReportLog",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result > 0;
        }
    }
}