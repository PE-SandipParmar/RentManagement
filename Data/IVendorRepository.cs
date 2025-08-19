using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IVendorRepository
    {
        Task<IEnumerable<Vendor>> GetAllVendorsAsync();
        Task<Vendor?> GetVendorByIdAsync(int id);
        Task<int> AddVendorAsync(Vendor vendor);
        Task<bool> UpdateVendorAsync(Vendor vendor);
        Task<bool> DeleteVendorAsync(int id);
        Task<IEnumerable<Vendor>> SearchVendorsAsync(string searchTerm, string statusFilter, int pageNumber, int pageSize);
        Task<int> GetVendorCountAsync(string searchTerm, string statusFilter);
    }
}
