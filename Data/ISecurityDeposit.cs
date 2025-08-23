using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentManagement.Data
{
    public interface ISecurityDepositRepository
    {
        Task<int> CreateAsync(SecurityDeposit securityDeposit);
        Task<SecurityDeposit?> GetByIdAsync(int id);
        Task<PagedResult<SecurityDeposit>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<bool> UpdateAsync(SecurityDeposit securityDeposit);
        Task<bool> DeleteAsync(int id,int ModifiedBy);

        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNamesAsync();
        Task ToggleActiveStatus(int Id);
        Task<decimal> GetEmployeeSalaryAsync(int employeeId);
        Task<List<dynamic>> GetLeasesByEmployeeAsync(int employeeId);
        Task<int> GetLeaseOwnerAsync(int leaseId);
        Task<bool> IsDuplicateRecordAsync(int employeeId, int leaseId, int vendorId, int? excludeId = null);

    }
}
