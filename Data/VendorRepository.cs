using RentManagement.Models;
using System.Data.SqlClient;
using System.Data;
using Dapper;

namespace RentManagement.Data
{
    public class VendorRepository : IVendorRepository
    {
        private readonly string _connectionString;

        public VendorRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<IEnumerable<Vendor>> GetAllVendorsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Vendor>("sp_GetAllVendors", commandType: CommandType.StoredProcedure);
        }

        public async Task<Vendor?> GetVendorByIdAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            return await connection.QueryFirstOrDefaultAsync<Vendor>("sp_GetVendorById", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddVendorAsync(Vendor vendor)
        {
            using var connection = CreateConnection();
            var parameters = new
            {
                VendorCode = vendor.VendorCode,
                VendorName = vendor.VendorName,
                PANNumber = vendor.PANNumber,
                MobileNumber = vendor.MobileNumber,
                AlternateNumber = vendor.AlternateNumber,
                EmailId = vendor.EmailId,
                Address = vendor.Address,
                AccountHolderName = vendor.AccountHolderName,
                BankName = vendor.BankName,
                BranchName = vendor.BranchName,
                AccountNumber = vendor.AccountNumber,
                IFSCCode = vendor.IFSCCode,
                PropertyAddress = vendor.PropertyAddress,
                TotalRentAmount = vendor.TotalRentAmount,
                LinkedEmployees = vendor.LinkedEmployees,
                Status = vendor.Status
            };

            var result = await connection.QuerySingleAsync<int>("sp_InsertVendor", parameters, commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<bool> UpdateVendorAsync(Vendor vendor)
        {
            using var connection = CreateConnection();
            var parameters = new
            {
                Id = vendor.Id,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.VendorName,
                PANNumber = vendor.PANNumber,
                MobileNumber = vendor.MobileNumber,
                AlternateNumber = vendor.AlternateNumber,
                EmailId = vendor.EmailId,
                Address = vendor.Address,
                AccountHolderName = vendor.AccountHolderName,
                BankName = vendor.BankName,
                BranchName = vendor.BranchName,
                AccountNumber = vendor.AccountNumber,
                IFSCCode = vendor.IFSCCode,
                PropertyAddress = vendor.PropertyAddress,
                TotalRentAmount = vendor.TotalRentAmount,
                LinkedEmployees = vendor.LinkedEmployees,
                Status = vendor.Status
            };

            var affectedRows = await connection.ExecuteAsync("sp_UpdateVendor", parameters, commandType: CommandType.StoredProcedure);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteVendorAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            var affectedRows = await connection.ExecuteAsync("sp_DeleteVendor", parameters, commandType: CommandType.StoredProcedure);
            return affectedRows > 0;
        }


        public async Task<IEnumerable<Vendor>> SearchVendorsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY CreatedDate DESC) as RowNum
                    FROM Vendors 
                    WHERE (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR VendorName LIKE '%' + @SearchTerm + '%' 
                           OR VendorCode LIKE '%' + @SearchTerm + '%'
                           OR MobileNumber LIKE '%' + @SearchTerm + '%')
                    AND (@StatusFilter IS NULL OR @StatusFilter = '' OR Status = @StatusFilter)
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Vendor>(sql, parameters);
        }

        public async Task<int> GetVendorCountAsync(string searchTerm, string statusFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Vendors 
                WHERE (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR VendorName LIKE '%' + @SearchTerm + '%' 
                       OR VendorCode LIKE '%' + @SearchTerm + '%'
                       OR MobileNumber LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter IS NULL OR @StatusFilter = '' OR Status = @StatusFilter)";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }
    }
}
