using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IPropertyRepository
    {
        // Basic CRUD operations
        Task<IEnumerable<Property>> GetAllPropertiesAsync();
        Task<IEnumerable<Property>> GetAllPropertiesWithApprovalStatusAsync(string searchTerm, string statusFilter, int? vendorFilter, int pageNumber, int pageSize);
        Task<int> GetAllPropertiesWithApprovalStatusCountAsync(string searchTerm, string statusFilter, int? vendorFilter);
        Task<Property?> GetPropertyByIdAsync(int id);
        Task<Property?> GetPropertyByCodeAsync(string propertyCode);
        Task<string> GetNextPropertyCodeAsync();
        Task<int> AddPropertyAsync(Property property);
        Task<bool> UpdatePropertyAsync(Property property);
        Task<bool> DeletePropertyAsync(int id);
        Task<IEnumerable<Property>> SearchPropertiesAsync(string searchTerm, string statusFilter, int? vendorFilter, int pageNumber, int pageSize);
        Task<int> GetPropertyCountAsync(string searchTerm, string statusFilter, int? vendorFilter);

        // Approval workflow methods
        Task<IEnumerable<Property>> GetApprovedPropertiesAsync(string searchTerm, string statusFilter, int? vendorFilter, int pageNumber, int pageSize);
        Task<int> GetApprovedPropertyCountAsync(string searchTerm, string statusFilter, int? vendorFilter);

        Task<IEnumerable<Property>> GetPendingApprovalsAsync(string searchTerm, int pageNumber, int pageSize);
        Task<int> GetPendingApprovalCountAsync(string searchTerm);

        Task<IEnumerable<Property>> GetRejectedPropertiesAsync(string searchTerm, int pageNumber, int pageSize);
        Task<int> GetRejectedPropertyCountAsync(string searchTerm);

        Task<bool> ApprovePropertyAsync(int id, string checkerUserId, string checkerUserName);
        Task<bool> RejectPropertyAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason);

        Task<int> AddPropertyForApprovalAsync(Property property, string makerUserId, string makerUserName, MakerAction action);
        Task<bool> UpdatePropertyForApprovalAsync(Property property, string makerUserId, string makerUserName);
        Task<bool> DeletePropertyForApprovalAsync(int id, string makerUserId, string makerUserName);

        Task<Property?> GetOriginalPropertyForUpdateAsync(int id);
        Task<bool> HasPendingChangesAsync(int id);

        // Additional helper methods
        Task<IEnumerable<Property>> GetPropertiesByVendorAsync(int vendorId);
        Task<IEnumerable<Property>> GetPropertiesByEmployeeAsync(string employeeName);
    }
}