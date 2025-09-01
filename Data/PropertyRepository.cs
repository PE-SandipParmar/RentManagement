using RentManagement.Models;
using System.Data.SqlClient;
using System.Data;
using Dapper;

namespace RentManagement.Data
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly string _connectionString;

        public PropertyRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<Property>> GetAllPropertiesAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Property>("sp_GetAllProperties", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Property>> GetAllPropertiesWithApprovalStatusAsync(string searchTerm, string statusFilter, int? vendorFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var offset = (pageNumber - 1) * pageSize;

            var sql = @"
                SELECT 
                    p.*,
                    v.VendorCode,
                    v.VendorName,
                    CASE 
                        WHEN p.ApprovalStatus = 1 THEN 'Pending'
                        WHEN p.ApprovalStatus = 2 THEN 'Approved'
                        WHEN p.ApprovalStatus = 3 THEN 'Rejected'
                        ELSE 'Unknown'
                    END as ApprovalStatusText
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.IsActiveRecord = 1
                AND (@SearchTerm = '' OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' OR p.PropertyCode LIKE '%' + @SearchTerm + '%' OR v.VendorName LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter = '' OR p.Status = @StatusFilter)
                AND (@VendorFilter IS NULL OR p.VendorId = @VendorFilter)
                ORDER BY p.CreatedDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var parameters = new
            {
                SearchTerm = searchTerm ?? "",
                StatusFilter = statusFilter ?? "",
                VendorFilter = vendorFilter,
                Offset = offset,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Property>(sql, parameters);
        }

        public async Task<int> GetAllPropertiesWithApprovalStatusCountAsync(string searchTerm, string statusFilter, int? vendorFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.IsActiveRecord = 1
                AND (@SearchTerm = '' OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' OR p.PropertyCode LIKE '%' + @SearchTerm + '%' OR v.VendorName LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter = '' OR p.Status = @StatusFilter)
                AND (@VendorFilter IS NULL OR p.VendorId = @VendorFilter)";

            var parameters = new
            {
                SearchTerm = searchTerm ?? "",
                StatusFilter = statusFilter ?? "",
                VendorFilter = vendorFilter
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<Property?> GetPropertyByIdAsync(int id)
        {
            using var connection = CreateConnection();
            var sql = @"
                SELECT p.*, v.VendorCode, v.VendorName 
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.Id = @Id AND p.IsActiveRecord = 1";

            var parameters = new { Id = id };
            return await connection.QueryFirstOrDefaultAsync<Property>(sql, parameters);
        }

        public async Task<Property?> GetPropertyByCodeAsync(string propertyCode)
        {
            using var connection = CreateConnection();
            var sql = "SELECT * FROM Properties WHERE PropertyCode = @PropertyCode AND IsActiveRecord = 1";
            var parameters = new { PropertyCode = propertyCode };
            return await connection.QueryFirstOrDefaultAsync<Property>(sql, parameters);
        }

        public async Task<string> GetNextPropertyCodeAsync()
        {
            using var connection = CreateConnection();

            // Get the highest property code number
            var sql = @"
                SELECT TOP 1 PropertyCode 
                FROM Properties 
                WHERE PropertyCode LIKE 'PROP%' 
                AND IsActiveRecord = 1 
                ORDER BY CAST(SUBSTRING(PropertyCode, 5, LEN(PropertyCode) - 4) AS INT) DESC";

            var lastCode = await connection.QueryFirstOrDefaultAsync<string>(sql);

            if (string.IsNullOrEmpty(lastCode))
            {
                // If no existing codes, start with PROP0001
                return "PROP0001";
            }

            // Extract the number part and increment
            var numberPart = lastCode.Substring(4); // Remove "PROP" prefix
            if (int.TryParse(numberPart, out int lastNumber))
            {
                var nextNumber = lastNumber + 1;
                return $"PROP{nextNumber:D4}"; // Format as 4 digits with leading zeros
            }

            // Fallback if parsing fails
            return "PROP0001";
        }

        public async Task<int> AddPropertyAsync(Property property)
        {
            using var connection = CreateConnection();

            var sql = @"
                INSERT INTO Properties (
                    PropertyCode, VendorId, PropertyAddress, TotalRentAmount,
                    LinkedEmployees, Status, ApprovalStatus, MakerUserId, MakerUserName,
                    MakerAction, CheckerUserId, CheckerUserName, ApprovalDate, 
                    RejectionReason, IsActiveRecord, CreatedDate, UpdatedDate
                )
                VALUES (
                    @PropertyCode, @VendorId, @PropertyAddress, @TotalRentAmount,
                    @LinkedEmployees, @Status, @ApprovalStatus, @MakerUserId, @MakerUserName,
                    @MakerAction, @CheckerUserId, @CheckerUserName, @ApprovalDate,
                    @RejectionReason, @IsActiveRecord, GETDATE(), GETDATE()
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new
            {
                PropertyCode = property.PropertyCode,
                VendorId = property.VendorId,
                PropertyAddress = property.PropertyAddress,
                TotalRentAmount = property.TotalRentAmount,
                LinkedEmployees = property.LinkedEmployees,
                Status = property.Status,
                ApprovalStatus = (int)property.ApprovalStatus,
                MakerUserId = property.MakerUserId,
                MakerUserName = property.MakerUserName,
                MakerAction = (int)property.MakerAction,
                CheckerUserId = property.CheckerUserId,
                CheckerUserName = property.CheckerUserName,
                ApprovalDate = property.ApprovalDate,
                RejectionReason = property.RejectionReason,
                IsActiveRecord = property.IsActiveRecord
            };

            var result = await connection.QuerySingleAsync<int>(sql, parameters);
            return result;
        }

        public async Task<bool> UpdatePropertyAsync(Property property)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Properties
                SET 
                    VendorId = @VendorId,
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
                Id = property.Id,
                VendorId = property.VendorId,
                PropertyAddress = property.PropertyAddress,
                TotalRentAmount = property.TotalRentAmount,
                LinkedEmployees = property.LinkedEmployees,
                Status = property.Status,
                ApprovalStatus = (int)property.ApprovalStatus,
                MakerUserId = property.MakerUserId,
                MakerUserName = property.MakerUserName,
                CheckerUserId = property.CheckerUserId,
                CheckerUserName = property.CheckerUserName,
                MakerAction = (int)property.MakerAction,
                ApprovalDate = property.ApprovalDate,
                RejectionReason = property.RejectionReason,
                IsActiveRecord = property.IsActiveRecord
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> DeletePropertyAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            var affectedRows = await connection.ExecuteAsync("sp_DeleteProperty", parameters, commandType: CommandType.StoredProcedure);
            return affectedRows > 0;
        }

        public async Task<IEnumerable<Property>> SearchPropertiesAsync(string searchTerm, string statusFilter, int? vendorFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT p.*, v.VendorCode, v.VendorName, 
                           ROW_NUMBER() OVER (ORDER BY p.CreatedDate DESC) as RowNum
                    FROM Properties p
                    INNER JOIN Vendors v ON p.VendorId = v.Id
                    WHERE p.IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                           OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                           OR v.VendorName LIKE '%' + @SearchTerm + '%')
                    AND (@StatusFilter IS NULL OR @StatusFilter = '' OR p.Status = @StatusFilter)
                    AND (@VendorFilter IS NULL OR p.VendorId = @VendorFilter)
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                VendorFilter = vendorFilter,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Property>(sql, parameters);
        }

        public async Task<int> GetPropertyCountAsync(string searchTerm, string statusFilter, int? vendorFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                       OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                       OR v.VendorName LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter IS NULL OR @StatusFilter = '' OR p.Status = @StatusFilter)
                AND (@VendorFilter IS NULL OR p.VendorId = @VendorFilter)";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                VendorFilter = vendorFilter
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        #endregion

        #region Approval Workflow Methods

        public async Task<IEnumerable<Property>> GetApprovedPropertiesAsync(string searchTerm, string statusFilter, int? vendorFilter, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT p.*, v.VendorCode, v.VendorName,
                           ROW_NUMBER() OVER (ORDER BY p.ApprovalDate DESC, p.CreatedDate DESC) as RowNum
                    FROM Properties p
                    INNER JOIN Vendors v ON p.VendorId = v.Id
                    WHERE p.ApprovalStatus = 2 AND p.IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                           OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                           OR v.VendorName LIKE '%' + @SearchTerm + '%')
                    AND (@StatusFilter IS NULL OR @StatusFilter = '' OR p.Status = @StatusFilter)
                    AND (@VendorFilter IS NULL OR p.VendorId = @VendorFilter)
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                VendorFilter = vendorFilter,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Property>(sql, parameters);
        }

        public async Task<int> GetApprovedPropertyCountAsync(string searchTerm, string statusFilter, int? vendorFilter)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.ApprovalStatus = 2 AND p.IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                       OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                       OR v.VendorName LIKE '%' + @SearchTerm + '%')
                AND (@StatusFilter IS NULL OR @StatusFilter = '' OR p.Status = @StatusFilter)
                AND (@VendorFilter IS NULL OR p.VendorId = @VendorFilter)";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                StatusFilter = string.IsNullOrEmpty(statusFilter) ? null : statusFilter,
                VendorFilter = vendorFilter
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<IEnumerable<Property>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT p.*, v.VendorCode, v.VendorName,
                           ROW_NUMBER() OVER (ORDER BY p.CreatedDate DESC) as RowNum
                    FROM Properties p
                    INNER JOIN Vendors v ON p.VendorId = v.Id
                    WHERE p.ApprovalStatus = 1 AND p.IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                           OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                           OR v.VendorName LIKE '%' + @SearchTerm + '%'
                           OR p.MakerUserName LIKE '%' + @SearchTerm + '%')
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Property>(sql, parameters);
        }

        public async Task<int> GetPendingApprovalCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.ApprovalStatus = 1 AND p.IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                       OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                       OR v.VendorName LIKE '%' + @SearchTerm + '%'
                       OR p.MakerUserName LIKE '%' + @SearchTerm + '%')";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<IEnumerable<Property>> GetRejectedPropertiesAsync(string searchTerm, int pageNumber, int pageSize)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT * FROM (
                    SELECT p.*, v.VendorCode, v.VendorName,
                           ROW_NUMBER() OVER (ORDER BY p.ApprovalDate DESC) as RowNum
                    FROM Properties p
                    INNER JOIN Vendors v ON p.VendorId = v.Id
                    WHERE p.ApprovalStatus = 3 AND p.IsActiveRecord = 1
                    AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                           OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                           OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                           OR v.VendorName LIKE '%' + @SearchTerm + '%')
                ) AS PagedResults
                WHERE RowNum BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
                ORDER BY RowNum";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await connection.QueryAsync<Property>(sql, parameters);
        }

        public async Task<int> GetRejectedPropertyCountAsync(string searchTerm)
        {
            using var connection = CreateConnection();

            var sql = @"
                SELECT COUNT(*)
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.ApprovalStatus = 3 AND p.IsActiveRecord = 1
                AND (@SearchTerm IS NULL OR @SearchTerm = '' 
                       OR p.PropertyAddress LIKE '%' + @SearchTerm + '%' 
                       OR p.PropertyCode LIKE '%' + @SearchTerm + '%'
                       OR v.VendorName LIKE '%' + @SearchTerm + '%')";

            var parameters = new
            {
                SearchTerm = string.IsNullOrEmpty(searchTerm) ? null : searchTerm
            };

            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        public async Task<bool> ApprovePropertyAsync(int id, string checkerUserId, string checkerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Properties 
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

        public async Task<bool> RejectPropertyAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Properties 
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

        public async Task<int> AddPropertyForApprovalAsync(Property property, string makerUserId, string makerUserName, MakerAction action)
        {
            property.ApprovalStatus = ApprovalStatus.Pending;
            property.MakerUserId = makerUserId;
            property.MakerUserName = makerUserName;
            property.MakerAction = action;
            property.IsActiveRecord = true;

            return await AddPropertyAsync(property);
        }

        public async Task<bool> UpdatePropertyForApprovalAsync(Property property, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Properties 
                SET 
                    VendorId = @VendorId,
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
                Id = property.Id,
                VendorId = property.VendorId,
                PropertyAddress = property.PropertyAddress,
                TotalRentAmount = property.TotalRentAmount,
                LinkedEmployees = property.LinkedEmployees,
                Status = property.Status,
                MakerUserId = makerUserId,
                MakerUserName = makerUserName
            };

            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public async Task<bool> DeletePropertyForApprovalAsync(int id, string makerUserId, string makerUserName)
        {
            using var connection = CreateConnection();

            var sql = @"
                UPDATE Properties 
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

        public async Task<Property?> GetOriginalPropertyForUpdateAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            return await connection.QueryFirstOrDefaultAsync<Property>(
                "SELECT * FROM Properties WHERE Id = @Id AND ApprovalStatus = 2",
                parameters);
        }

        public async Task<bool> HasPendingChangesAsync(int id)
        {
            using var connection = CreateConnection();
            var parameters = new { Id = id };
            var count = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM Properties WHERE Id = @Id AND ApprovalStatus = 1",
                parameters);
            return count > 0;
        }

        #endregion

        #region Additional Helper Methods

        public async Task<IEnumerable<Property>> GetPropertiesByVendorAsync(int vendorId)
        {
            using var connection = CreateConnection();
            var sql = @"
                SELECT p.*, v.VendorCode, v.VendorName 
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.VendorId = @VendorId 
                AND p.ApprovalStatus = 2 
                AND p.IsActiveRecord = 1
                ORDER BY p.PropertyCode";

            var parameters = new { VendorId = vendorId };
            return await connection.QueryAsync<Property>(sql, parameters);
        }

        public async Task<IEnumerable<Property>> GetPropertiesByEmployeeAsync(string employeeName)
        {
            using var connection = CreateConnection();
            var sql = @"
                SELECT p.*, v.VendorCode, v.VendorName 
                FROM Properties p
                INNER JOIN Vendors v ON p.VendorId = v.Id
                WHERE p.LinkedEmployees LIKE '%' + @EmployeeName + '%'
                AND p.ApprovalStatus = 2 
                AND p.IsActiveRecord = 1
                ORDER BY p.PropertyCode";

            var parameters = new { EmployeeName = employeeName };
            return await connection.QueryAsync<Property>(sql, parameters);
        }

        #endregion
    }
}