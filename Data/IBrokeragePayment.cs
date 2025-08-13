using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;
using System.Numerics;

namespace RentManagement.Data
{
    public interface IBrokeragePaymentRepository
    {

        Task<int> CreateAsync(BrokeragePayment BrokeragePayment);
        Task<BrokeragePayment?> GetByIdAsync(int id);
        Task<PagedResult<BrokeragePayment>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);

        Task<bool> UpdateAsync(BrokeragePayment payment);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<TdsApplicable>> GetTdsApplicableAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNameAsync();
    }
}

