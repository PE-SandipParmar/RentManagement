using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IEmployeeRepository
    {
        // Original methods
        Task<PagedResult<Employee>> GetEmployeesAsync(int page, int pageSize, string search);
        Task<IEnumerable<Employee>> GetAllEmployeesDropdownAsync();
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<bool> AadharExistsAsync(string aadhar, int? excludeId = null);
        Task<int> CreateEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<IEnumerable<Department>> GetDepartmentsAsync();
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task<IEnumerable<Designation>> GetDesignationsAsync();
        Task<IEnumerable<Designation>> GetAllDesignationsAsync();
        Task ToggleActiveStatus(int? Id);

        // Approval Workflow Methods
        Task<IEnumerable<Employee>> GetApprovedEmployeesAsync(string searchTerm = "", string statusFilter = "", int page = 1, int pageSize = 10);
        Task<int> GetApprovedEmployeeCountAsync(string searchTerm = "", string statusFilter = "");
        Task<IEnumerable<Employee>> GetPendingApprovalsAsync(string searchTerm = "", int page = 1, int pageSize = 10);
        Task<int> GetPendingApprovalCountAsync(string searchTerm = "");
        Task<IEnumerable<Employee>> GetRejectedEmployeesAsync(string searchTerm = "", int page = 1, int pageSize = 10);
        Task<int> GetRejectedEmployeeCountAsync(string searchTerm = "");
        Task<IEnumerable<Employee>> GetAllEmployeesWithApprovalStatusAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize);
        Task<int> GetAllEmployeesWithApprovalStatusCountAsync(string searchTerm, string statusFilter);
        Task<int> AddEmployeeForApprovalAsync(Employee employee, string makerUserId, string makerUserName, MakerAction makerAction);
        Task<bool> UpdateEmployeeForApprovalAsync(Employee employee, string makerUserId, string makerUserName);
        Task<bool> DeleteEmployeeForApprovalAsync(int employeeId, string makerUserId, string makerUserName);
        Task<bool> ApproveEmployeeAsync(int employeeId, string checkerUserId, string checkerUserName);
        Task<bool> RejectEmployeeAsync(int employeeId, string checkerUserId, string checkerUserName, string rejectionReason);
        Task<bool> HasPendingChangesAsync(int employeeId);
        Task<Employee?> GetEmployeeByCodeAsync(string employeeCode);
        Task<IEnumerable<Employee>> GetEmployeesByIdsAsync(List<int> employeeIds);
    }
}