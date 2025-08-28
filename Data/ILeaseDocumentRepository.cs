using RentManagement.Models;
using RentManagement.Models.RentPaymentSystem.Models;

namespace RentManagement.Data
{
    public interface ILeaseDocumentRepository
    {
        Task<int> AddLeaseDocumentAsync(LeaseDocument leaseDocument);
        Task<List<LeaseDocument>> GetLeaseDocumentsByLeaseIdAsync(int leaseId);
        Task<LeaseDocument> GetLeaseDocumentByIdAsync(int id);
        Task<bool> DeleteLeaseDocumentAsync(int id);
        Task<bool> UpdateLeaseDocumentAsync(LeaseDocument leaseDocument);

    }
}
