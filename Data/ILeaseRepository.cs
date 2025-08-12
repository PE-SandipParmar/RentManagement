using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;

namespace RentManagement.Data
{
    public interface ILeaseRepository
    {
        // Create a new Lease
        Task<int> CreateLeaseAsync(Lease lease);

        // Read - get a Lease by Id
        Task<Lease?> GetLeaseByIdAsync(int id);

        // Read - get paged list of Leases with optional search/filter and pagination
        Task<PagedResult<Lease>> GetLeasesAsync(int pageNumber, int pageSize, string? searchTerm);

        // Update an existing Lease
        Task<bool> UpdateLeaseAsync(Lease lease);

        // Delete a Lease by Id
        Task<bool> DeleteLeaseAsync(int id);

        // Optional: Count total leases matching search/filter criteria (for pagination)
        Task<int> GetLeasesCountAsync(string? searchTerm);


        Task<IEnumerable<LeaseType>> GetLeaseTypesAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<RentRecoveryElement>> GetRentRecoveryElementsAsync();
        Task<IEnumerable<LicenseFeeRecoveryElement>> GetLicenseFeeRecoveryElementsAsync();
        Task<IEnumerable<PaymentTerm>> GetPaymentTermsAsync();
        Task<IEnumerable<PayableOnOrBeforeOption>> GetPayableOnOrBeforeOptionsAsync();
        Task<IEnumerable<PerquisiteApplicablePercent>> GetPerquisiteApplicablePercentsAsync();

    }
}

