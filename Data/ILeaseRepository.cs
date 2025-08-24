using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;

namespace RentManagement.Data
{
    public interface ILeaseRepository
    {
        // Basic CRUD operations
        Task<int> CreateLeaseAsync(Lease lease);
        Task<Lease?> GetLeaseByIdAsync(int id);
        Task<PagedResult<Lease>> GetLeasesAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<bool> UpdateLeaseAsync(Lease lease);
        Task<bool> DeleteLeaseAsync(int id);
        Task<int> GetLeasesCountAsync(string? searchTerm);
        Task<bool> LeaseNoExistsAsync(string leaseNo, int? excludeId = null);
        Task<Lease?> GetLeaseByRefNoAsync(string refNo);

        // Approval Workflow Methods
        Task<IEnumerable<Lease>> GetApprovedLeasesAsync(string searchTerm, string statusFilter, int page, int pageSize);
        Task<int> GetApprovedLeaseCountAsync(string searchTerm, string statusFilter);
        Task<IEnumerable<Lease>> GetPendingApprovalsAsync(string searchTerm, int page, int pageSize);
        Task<int> GetPendingApprovalCountAsync(string searchTerm);
        Task<IEnumerable<Lease>> GetRejectedLeasesAsync(string searchTerm, int page, int pageSize);
        Task<int> GetRejectedLeaseCountAsync(string searchTerm);
        Task<int> AddLeaseForApprovalAsync(Lease lease, string makerUserId, string makerUserName, MakerAction action);
        Task<bool> UpdateLeaseForApprovalAsync(Lease lease, string makerUserId, string makerUserName);
        Task<bool> DeleteLeaseForApprovalAsync(int id, string makerUserId, string makerUserName);
        Task<bool> ApproveLeaseAsync(int id, string checkerUserId, string checkerUserName);
        Task<bool> RejectLeaseAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason);
        Task<bool> HasPendingChangesAsync(int id);

        // Dropdown data methods
        Task<IEnumerable<LeaseType>> GetLeaseTypesAsync();
        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<RentRecoveryElement>> GetRentRecoveryElementsAsync();
        Task<IEnumerable<LicenseFeeRecoveryElement>> GetLicenseFeeRecoveryElementsAsync();
        Task<IEnumerable<PaymentTerm>> GetPaymentTermsAsync();
        Task<IEnumerable<PayableOnOrBeforeOption>> GetPayableOnOrBeforeOptionsAsync();
        Task<IEnumerable<PerquisiteApplicablePercent>> GetPerquisiteApplicablePercentsAsync();

        Task<decimal?> GetEmployeeHRAAsync(int employeeId);
    }
}

