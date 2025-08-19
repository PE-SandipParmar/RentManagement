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
        Task<bool> DeleteAsync(int id);

        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNamesAsync();
        Task ToggleActiveStatus(int Id);

    }
}
