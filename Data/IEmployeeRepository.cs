using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IEmployeeRepository
    {
        Task<PagedResult<Employee>> GetEmployeesAsync(int page, int pageSize, string search);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<int> CreateEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<IEnumerable<Department>> GetDepartmentsAsync();
        Task<IEnumerable<Designation>> GetDesignationsAsync();

    }
}
