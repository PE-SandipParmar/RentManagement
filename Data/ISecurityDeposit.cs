using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentManagement.Data
{
    public interface ISecurityDepositRepository
    {
        // Existing methods
        Task<int> CreateAsync(SecurityDeposit securityDeposit);
        Task<SecurityDeposit?> GetByIdAsync(int id);
        Task<PagedResult<SecurityDeposit>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<bool> UpdateAsync(SecurityDeposit securityDeposit);
        Task<bool> DeleteAsync(int id, int ModifiedBy);

        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNamesAsync();
        Task ToggleActiveStatus(int Id);
        Task<decimal> GetEmployeeSalaryAsync(int employeeId);
        Task<List<dynamic>> GetLeasesByEmployeeAsync(int employeeId);
        Task<int> GetLeaseOwnerAsync(int leaseId);
        Task<bool> IsDuplicateRecordAsync(int employeeId, int leaseId, int vendorId, int? excludeId = null);

        // New approval workflow methods
        Task<IEnumerable<SecurityDeposit>> GetAllSecurityDepositsWithApprovalStatusAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize);
        Task<int> GetAllSecurityDepositsWithApprovalStatusCountAsync(string searchTerm, string statusFilter);

        Task<IEnumerable<SecurityDeposit>> GetApprovedSecurityDepositsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize);
        Task<int> GetApprovedSecurityDepositCountAsync(string searchTerm, string statusFilter);

        Task<IEnumerable<SecurityDeposit>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<int> GetPendingApprovalCountAsync(string searchTerm);

        Task<IEnumerable<SecurityDeposit>> GetRejectedSecurityDepositsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<int> GetRejectedSecurityDepositCountAsync(string searchTerm);

        Task<bool> ApproveSecurityDepositAsync(int id, string checkerUserId, string checkerUserName);
        Task<bool> RejectSecurityDepositAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason);

        Task<int> AddSecurityDepositForApprovalAsync(SecurityDeposit deposit, string makerUserId, string makerUserName, MakerAction action);
        Task<bool> UpdateSecurityDepositForApprovalAsync(SecurityDeposit deposit, string makerUserId, string makerUserName);
        Task<bool> DeleteSecurityDepositForApprovalAsync(int id, string makerUserId, string makerUserName);

        Task<SecurityDeposit?> GetOriginalSecurityDepositForUpdateAsync(int id);
        Task<bool> HasPendingChangesAsync(int id);
    }
}