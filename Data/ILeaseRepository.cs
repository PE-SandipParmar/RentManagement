using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;

namespace RentManagement.Data
{
    public interface ILeaseRepository
    {
        Task<int> CreateLeaseAsync(Lease lease);

        Task<Lease?> GetLeaseByIdAsync(int id);

        Task<PagedResult<Lease>> GetLeasesAsync(int pageNumber, int pageSize, string? searchTerm);

        Task<bool> UpdateLeaseAsync(Lease lease);

        Task<bool> DeleteLeaseAsync(int id);

        Task<int> GetLeasesCountAsync(string? searchTerm);

        Task<bool> LeaseNoExistsAsync(string leaseno, int? excludeId = null);

        Task<IEnumerable<LeaseType>> GetLeaseTypesAsync();
        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<RentRecoveryElement>> GetRentRecoveryElementsAsync();
        Task<IEnumerable<LicenseFeeRecoveryElement>> GetLicenseFeeRecoveryElementsAsync();
        Task<IEnumerable<PaymentTerm>> GetPaymentTermsAsync();
        Task<IEnumerable<PayableOnOrBeforeOption>> GetPayableOnOrBeforeOptionsAsync();
        Task<IEnumerable<PerquisiteApplicablePercent>> GetPerquisiteApplicablePercentsAsync();

    }
}

