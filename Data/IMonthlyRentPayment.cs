using RentManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;
using System.Numerics;

namespace RentManagement.Data
{
    public interface IMonthlyRentPaymentRepository
    {

        Task<int> CreateAsync(MonthlyRentPayment MonthlyRentPayment);
        Task<MonthlyRentPayment?> GetByIdAsync(int id);
        Task<PagedResult<MonthlyRentPayment>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);

        Task<bool> UpdateAsync(MonthlyRentPayment payment);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<TdsApplicable>> GetTdsApplicableAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNameAsync();
    }
}

