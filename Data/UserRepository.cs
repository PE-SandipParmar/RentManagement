using Dapper;
using RentManagement.Models;
using System.Data.SqlClient;

namespace RentManagement.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<PagedResult<User>> GetUsersAsync(int pageNumber, int pageSize, string searchTerm = "")
        {
            using var connection = new SqlConnection(_connectionString);

            var offset = (pageNumber - 1) * pageSize;
            var whereClause = string.IsNullOrEmpty(searchTerm) ? "" :
                "WHERE FirstName LIKE @SearchTerm OR LastName LIKE @SearchTerm OR Email LIKE @SearchTerm";

            var countQuery = $"SELECT COUNT(*) FROM Users {whereClause}";
            var dataQuery = $@"
                SELECT * FROM Users 
                {whereClause}
                ORDER BY CreatedAt DESC 
                OFFSET @Offset ROWS 
                FETCH NEXT @PageSize ROWS ONLY";

            var parameters = new
            {
                SearchTerm = $"%{searchTerm}%",
                Offset = offset,
                PageSize = pageSize
            };

            var totalItems = await connection.QuerySingleAsync<int>(countQuery, parameters);
            var users = await connection.QueryAsync<User>(dataQuery, parameters);

            return new PagedResult<User>
            {
                Items = users.ToList(),
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO Users (FirstName, LastName, Email, Phone, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@FirstName, @LastName, @Email, @Phone, @CreatedAt)";

            user.CreatedAt = DateTime.UtcNow;
            return await connection.QuerySingleAsync<int>(sql, user);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE Users 
                SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
                    Phone = @Phone, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            user.UpdatedAt = DateTime.UtcNow;
            var rowsAffected = await connection.ExecuteAsync(sql, user);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM Users WHERE Id = @Id", new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = excludeUserId.HasValue
                ? "SELECT COUNT(*) FROM Users WHERE Email = @Email AND Id != @ExcludeUserId"
                : "SELECT COUNT(*) FROM Users WHERE Email = @Email";

            var count = await connection.QuerySingleAsync<int>(sql,
                new { Email = email, ExcludeUserId = excludeUserId });
            return count > 0;
        }
    }
}
