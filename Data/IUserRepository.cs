using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IUserRepository
    {
        Task<PagedResult<User>> GetUsersAsync(int pageNumber, int pageSize, string searchTerm = "");
        Task<User?> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(User user);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    }
}
