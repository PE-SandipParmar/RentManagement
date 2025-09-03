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

        #region Existing Methods (Updated for Approval Workflow)

        public async Task<IEnumerable<Vendor>> GetAllVendorsAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Vendor>("sp_GetAllVendors", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Vendor>> GetAllVendorsWithApprovalStatusAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();
            
            var offset = (pageNumber - 1) * pageSize;
            
            var sql = @"
                SELECT 
                    v.*,
                    CASE 
                        WHEN v.ApprovalStatus = 1 THEN 'Pending'
                        WHEN v.ApprovalStatus = 2 THEN 'Approved'
                        WHEN v.ApprovalStatus = 3 THEN 'Rejected'
                        ELSE 'Unknown'
                    END as ApprovalStatusText
                FROM Vendors v
                WHERE v.IsActiveRecord = 1
                AND (@SearchTerm = '' OR v.VendorName LIKE '%' + @SearchTerm + '%' OR v.VendorCode LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter = '' OR v.Status = @StatusFilter)
                ORDER BY v.CreatedDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var parameters = new
            {
                SearchTerm = searchTerm ?? "",
                StatusFilter = statusFilter ?? "",
                Offset = offset,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Vendor>(sql, parameters);
        }

        public async Task<int> GetAllVendorsWithApprovalStatusCountAsync(string searchTerm, string statusFilter)
        {
            using var connection = CreateConnection();
            
            var sql = @"
                SELECT COUNT(*)
                FROM Vendors v
                WHERE v.IsActiveRecord = 1
                AND (@SearchTerm = '' OR v.VendorName LIKE '%' + @SearchTerm + '%' OR v.VendorCode LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter = '' OR v.Status = @StatusFilter)";

            var parameters = new
            {
                SearchTerm = searchTerm ?? "",
                StatusFilter = statusFilter ?? ""
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<Vendor?> GetVendorByIdAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            return await connection.QueryFirstOrDefaultAsync<Vendor>("sp_GetVendorById", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<Vendor?> GetVendorByCodeAsync(string vendorCode)
        {
            using var connection = CreateConnection();
            var sql = "SELECT * FROM Vendors WHERE VendorCode = @VendorCode AND IsActiveRecord = 1";
            var parameters = new { VendorCode = vendorCode };
            return await connection.QueryFirstOrDefaultAsync<Vendor>(sql, parameters);
        }

        public async Task<string> GetNextVendorCodeAsync()
        {
            using var connection = CreateConnection();
            
            // Get the highest vendor code number
            var sql = @"
                SELECT TOP 1 VendorCode 
                FROM Vendors 
                WHERE VendorCode LIKE 'OWN%' 
                AND IsActiveRecord = 1 
                ORDER BY CAST(SUBSTRING(VendorCode, 4, LEN(VendorCode) - 4) AS INT) DESC";
            
            var lastCode = await connection.QueryFirstOrDefaultAsync<string>(sql);
            
            if (string.IsNullOrEmpty(lastCode))
            {
                // If no existing codes, start with VEN001
                return "OWN0001";
            }
            
            // Extract the number part and increment
            var numberPart = lastCode.Substring(3); // Remove "VEN" prefix
            if (int.TryParse(numberPart, out int lastNumber))
            {
                var nextNumber = lastNumber + 1;
                return $"OWN{nextNumber:D4}"; // Format as 3 digits with leading zeros
            }
            
            // Fallback if parsing fails
            return "OWN0001";
        }

        public async Task<int> AddVendorAsync(Vendor vendor)
        {
            using var connection = CreateConnection();

            var sql = @"
	DECLARE @NewCode NVARCHAR(20)
	SELECT @NewCode = 'OWN' + RIGHT('00000' + CAST(ISNULL(MAX(Id),0) + 1 AS VARCHAR), 5)
	FROM Vendors
	
                INSERT INTO Vendors (
                    VendorCode, VendorName, VendorRole, PANNumber, GSTNumber, MobileNumber, AlternateNumber,
                    EmailId, Address, AccountHolderName, BankName, BranchName,
                    AccountNumber, IFSCCode, PropertyAddress, TotalRentAmount,
                    LinkedEmployees, Status, ApprovalStatus, MakerUserId, MakerUserName,
                    MakerAction, CheckerUserId, CheckerUserName, ApprovalDate, 
                    RejectionReason, IsActiveRecord, CreatedDate, UpdatedDate
                )
                VALUES (
                    @NewCode, @VendorName, @VendorRole, @PANNumber, @GSTNumber, @MobileNumber, @AlternateNumber,
                    @EmailId, @Address, @AccountHolderName, @BankName, @BranchName,
                    @AccountNumber, @IFSCCode, @PropertyAddress, @TotalRentAmount,
                    @LinkedEmployees, @Status, @ApprovalStatus, @MakerUserId, @MakerUserName,
                    @MakerAction, @CheckerUserId, @CheckerUserName, @ApprovalDate,
                    @RejectionReason, @IsActiveRecord, GETDATE(), GETDATE()
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                VendorCode = vendor.VendorCode,
                VendorName = vendor.VendorName,
                VendorRole = vendor.VendorRole,
                PANNumber = vendor.PANNumber,
                GSTNumber = vendor.GSTNumber,
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
                Status = vendor.Status,
                ApprovalStatus = (int)vendor.ApprovalStatus,
                MakerUserId = vendor.MakerUserId,
                MakerUserName = vendor.MakerUserName,
                MakerAction = (int)vendor.MakerAction,
                CheckerUserId = vendor.CheckerUserId,
                CheckerUserName = vendor.CheckerUserName,
                ApprovalDate = vendor.ApprovalDate,
                RejectionReason = vendor.RejectionReason,
                IsActiveRecord = vendor.IsActiveRecord
            };

            var result = await connection.QuerySingleAsync<int>(sql, parameters);
            return result;
        }

        public async Task<bool> UpdateVendorAsync(Vendor vendor)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Vendors
                SET 
                    VendorCode = @VendorCode,
                    VendorName = @VendorName,
                    VendorRole = @VendorRole,
                    PANNumber = @PANNumber,
                    GSTNumber = @GSTNumber,
                    MobileNumber = @MobileNumber,
                    AlternateNumber = @AlternateNumber,
                    EmailId = @EmailId,
                    Address = @Address,
                    AccountHolderName = @AccountHolderName,
                    BankName = @BankName,
                    BranchName = @BranchName,
                    AccountNumber = @AccountNumber,
                    IFSCCode = @IFSCCode,
                    PropertyAddress = @PropertyAddress,
                    TotalRentAmount = @TotalRentAmount,
                    LinkedEmployees = @LinkedEmployees,
                    Status = @Status,
                    ApprovalStatus = @ApprovalStatus,
                    MakerUserId = @MakerUserId,
                    MakerUserName = @MakerUserName,
                    CheckerUserId = @CheckerUserId,
                    CheckerUserName = @CheckerUserName,
                    MakerAction = @MakerAction,
                    ApprovalDate = @ApprovalDate,
                    RejectionReason = @RejectionReason,
                    IsActiveRecord = @IsActiveRecord,
                    UpdatedDate = GETDATE()
                WHERE Id = @Id";

            var parameters = new
            {
                Id = vendor.Id,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.VendorName,
                VendorRole = vendor.VendorRole,
                PANNumber = vendor.PANNumber,
                GSTNumber = vendor.GSTNumber,
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
                Status = vendor.Status,
                ApprovalStatus = (int)vendor.ApprovalStatus,
                MakerUserId = vendor.MakerUserId,
                MakerUserName = vendor.MakerUserName,
                CheckerUserId = vendor.CheckerUserId,
                CheckerUserName = vendor.CheckerUserName,
                MakerAction = (int)vendor.MakerAction,
                ApprovalDate = vendor.ApprovalDate,
                RejectionReason = vendor.RejectionReason,
                IsActiveRecord = vendor.IsActiveRecord
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
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
                    WHERE IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
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
                WHERE IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
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

        #endregion

        #region Approval Workflow Methods

        public async Task<IEnumerable<Vendor>> GetApprovedVendorsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY ApprovalDate DESC, CreatedDate DESC) as RowNum
                    FROM Vendors 
                    WHERE ApprovalStatus = 2 AND IsActiveRecord = 1 AND isnull(VendorRole,'Owner') = 'Owner'
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
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

        public async Task<int> GetApprovedVendorCountAsync(string searchTerm, string statusFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Vendors 
                WHERE ApprovalStatus = 2 AND IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
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

        public async Task<IEnumerable<Vendor>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY CreatedDate DESC) as RowNum
                    FROM Vendors 
                    WHERE ApprovalStatus = 1 AND IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR VendorName LIKE '%' + @SearchTerm + '%' 
                           OR VendorCode LIKE '%' + @SearchTerm + '%'
                           OR MobileNumber LIKE '%' + @SearchTerm + '%'
                           OR MakerUserName LIKE '%' + @SearchTerm + '%')
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Vendor>(sql, parameters);
        }

        public async Task<int> GetPendingApprovalCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Vendors 
                WHERE ApprovalStatus = 1 AND IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR VendorName LIKE '%' + @SearchTerm + '%' 
                       OR VendorCode LIKE '%' + @SearchTerm + '%'
                       OR MobileNumber LIKE '%' + @SearchTerm + '%'
                       OR MakerUserName LIKE '%' + @SearchTerm + '%')";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<IEnumerable<Vendor>> GetRejectedVendorsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY ApprovalDate DESC) as RowNum
                    FROM Vendors 
                    WHERE ApprovalStatus = 3 AND IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR VendorName LIKE '%' + @SearchTerm + '%' 
                           OR VendorCode LIKE '%' + @SearchTerm + '%'
                           OR MobileNumber LIKE '%' + @SearchTerm + '%')
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Vendor>(sql, parameters);
        }

        public async Task<int> GetRejectedVendorCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Vendors 
                WHERE ApprovalStatus = 3 AND IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR VendorName LIKE '%' + @SearchTerm + '%' 
                       OR VendorCode LIKE '%' + @SearchTerm + '%'
                       OR MobileNumber LIKE '%' + @SearchTerm + '%')";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<bool> ApproveVendorAsync(int id, string checkerUserId, string checkerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Vendors 
                SET ApprovalStatus = 2, 
                    CheckerUserId = @CheckerUserId, 
                    CheckerUserName = @CheckerUserName, 
                    ApprovalDate = GETDATE(),
                    UpdatedDate = GETDATE()
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

        public async Task<bool> RejectVendorAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Vendors 
                SET ApprovalStatus = 3, 
                    CheckerUserId = @CheckerUserId, 
                    CheckerUserName = @CheckerUserName, 
                    ApprovalDate = GETDATE(),
                    RejectionReason = @RejectionReason,
                    UpdatedDate = GETDATE()
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

        public async Task<int> AddVendorForApprovalAsync(Vendor vendor, string makerUserId, string makerUserName, MakerAction action)
        {
            vendor.ApprovalStatus = ApprovalStatus.Pending;
            vendor.MakerUserId = makerUserId;
            vendor.MakerUserName = makerUserName;
            vendor.MakerAction = action;
            vendor.IsActiveRecord = true;

            return await AddVendorAsync(vendor);
        }

        public async Task<bool> UpdateVendorForApprovalAsync(Vendor vendor, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Vendors 
                SET 
                    VendorCode = @VendorCode,
                    VendorName = @VendorName,
                    VendorRole = @VendorRole,
                    PANNumber = @PANNumber,
                    MobileNumber = @MobileNumber,
                    AlternateNumber = @AlternateNumber,
                    EmailId = @EmailId,
                    Address = @Address,
                    AccountHolderName = @AccountHolderName,
                    BankName = @BankName,
                    BranchName = @BranchName,
                    AccountNumber = @AccountNumber,
                    IFSCCode = @IFSCCode,
                    PropertyAddress = @PropertyAddress,
                    TotalRentAmount = @TotalRentAmount,
                    LinkedEmployees = @LinkedEmployees,
                    Status = @Status,
                    ApprovalStatus = 1, 
                    MakerUserId = @MakerUserId, 
                    MakerUserName = @MakerUserName, 
                    MakerAction = 2,
                    CheckerUserId = NULL,
                    CheckerUserName = NULL,
                    ApprovalDate = NULL,
                    RejectionReason = NULL,
                    UpdatedDate = GETDATE()
                WHERE Id = @Id AND IsActiveRecord = 1";

            var parameters = new
            {
                Id = vendor.Id,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.VendorName,
                VendorRole = vendor.VendorRole,
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
                Status = vendor.Status,
                MakerUserId = makerUserId,
                MakerUserName = makerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteVendorForApprovalAsync(int id, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Vendors 
                SET ApprovalStatus = 1, 
                    MakerUserId = @MakerUserId, 
                    MakerUserName = @MakerUserName, 
                    MakerAction = 3,
                    CheckerUserId = NULL,
                    CheckerUserName = NULL,
                    ApprovalDate = NULL,
                    RejectionReason = NULL,
                    UpdatedDate = GETDATE()
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

        public async Task<Vendor?> GetOriginalVendorForUpdateAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            return await connection.QueryFirstOrDefaultAsync<Vendor>(
                "SELECT * FROM Vendors WHERE Id = @Id AND ApprovalStatus = 2",
                parameters);
        }

        public async Task<bool> HasPendingChangesAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            var count = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM Vendors WHERE Id = @Id AND ApprovalStatus = 1",
                parameters);
            return count > 0;
        }

        public async Task<IEnumerable<Vendor>> GetVendorsByEmployeeAsync(int employeeId)
        {
            try
            {
                using var connection = CreateConnection();
                
                var sql = @"
                    SELECT DISTINCT v.*
                    FROM Vendors v
                    INNER JOIN Properties p ON v.Id = p.VendorId
                    WHERE v.IsActiveRecord = 1 
                    AND v.ApprovalStatus = 2  -- Only approved vendors
                    AND v.Status = 'Active'   -- Only active vendors
                    AND p.IsActiveRecord = 1  -- Only active properties
                    AND p.ApprovalStatus = 2  -- Only approved properties
                    AND p.LinkedEmployees LIKE '%' + @EmployeeId + '%'  -- Employee is linked to property
                    ORDER BY v.VendorName";

                var parameters = new { EmployeeId = employeeId.ToString() };
                var result = await connection.QueryAsync<Vendor>(sql, parameters);
                return result ?? new List<Vendor>();
            }
            catch (Exception ex)
            {
                // Log the error (you might want to inject ILogger here)
                throw new Exception($"Error in GetVendorsByEmployeeAsync for employee ID {employeeId}: {ex.Message}", ex);
            }
        }

        #endregion
    }
}