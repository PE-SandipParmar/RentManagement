using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IVendorRepository
    {
        // Existing methods
        Task<IEnumerable<Vendor>> GetAllVendorsAsync();
        Task<Vendor?> GetVendorByIdAsync(int id);
        Task<Vendor?> GetVendorByCodeAsync(string vendorCode);
        Task<int> AddVendorAsync(Vendor vendor);
        Task<bool> UpdateVendorAsync(Vendor vendor);
        Task<bool> DeleteVendorAsync(int id);
        Task<IEnumerable<Vendor>> SearchVendorsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize);
        Task<int> GetVendorCountAsync(string searchTerm, string statusFilter);

        // New approval workflow methods
        Task<IEnumerable<Vendor>> GetApprovedVendorsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize);
        Task<int> GetApprovedVendorCountAsync(string searchTerm, string statusFilter);

        Task<IEnumerable<Vendor>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<int> GetPendingApprovalCountAsync(string searchTerm);

        Task<IEnumerable<Vendor>> GetRejectedVendorsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<int> GetRejectedVendorCountAsync(string searchTerm);

        Task<bool> ApproveVendorAsync(int id, string checkerUserId, string checkerUserName);
        Task<bool> RejectVendorAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason);

        Task<int> AddVendorForApprovalAsync(Vendor vendor, string makerUserId, string makerUserName, MakerAction action);
        Task<bool> UpdateVendorForApprovalAsync(Vendor vendor, string makerUserId, string makerUserName);
        Task<bool> DeleteVendorForApprovalAsync(int id, string makerUserId, string makerUserName);

        Task<Vendor?> GetOriginalVendorForUpdateAsync(int id);
        Task<bool> HasPendingChangesAsync(int id);
        // Add these methods to your IVendorRepository interface

        // Get pending approvals by specific maker

    }
}