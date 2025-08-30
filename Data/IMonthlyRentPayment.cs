using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RentManagement.Models;
using System.Numerics;

namespace RentManagement.Data
{
    public interface IMonthlyRentPaymentRepository
    {
        // Basic CRUD operations
        Task<int> CreateAsync(MonthlyRentPayment payment);
        Task<MonthlyRentPayment?> GetByIdAsync(int id);
        Task<PagedResult<MonthlyRentPayment>> GetAllAsync(int page, int pageSize, string? search);
        Task<bool> UpdateAsync(MonthlyRentPayment payment);
        Task<bool> DeleteAsync(int id);

        // Get all payments with filters (NEW METHODS)
        Task<IEnumerable<MonthlyRentPayment>> GetAllPaymentsAsync(string searchTerm, string statusFilter, string financialYearFilter, int pageNumber, int pageSize);
        Task<int> GetAllPaymentsCountAsync(string searchTerm, string statusFilter, string financialYearFilter);

        // Dropdown data methods
        Task<IEnumerable<EmployeeName>> GetEmployeeNamesAsync();
        Task<IEnumerable<Owner>> GetOwnersAsync();
        Task<IEnumerable<Owner>> GetOwnersByEmployeeAsync(int employeeid);
        Task<IEnumerable<Lease>> GetLeasesByEmployeeAndVendorAsync(int employeeId, int vendorId);
        Task<IEnumerable<TdsApplicable>> GetTdsApplicableAsync();
        Task<IEnumerable<LeaseName>> GetLeaseNameAsync();
        Task ToggleActiveStatus(int? Id);

        // Approval Workflow methods
        Task<IEnumerable<MonthlyRentPayment>> GetApprovedPaymentsAsync(string searchTerm, string statusFilter, string financialYearFilter, int pageNumber, int pageSize);
        Task<int> GetApprovedPaymentCountAsync(string searchTerm, string statusFilter, string financialYearFilter);
        Task<IEnumerable<MonthlyRentPayment>> GetPendingApprovalsAsync(string searchTerm, string financialYearFilter, int pageNumber, int pageSize);
        Task<int> GetPendingApprovalCountAsync(string searchTerm, string financialYearFilter);
        Task<IEnumerable<MonthlyRentPayment>> GetRejectedPaymentsAsync(string searchTerm, string financialYearFilter, int pageNumber, int pageSize);
        Task<int> GetRejectedPaymentCountAsync(string searchTerm, string financialYearFilter);
        Task<bool> ApprovePaymentAsync(int id, string checkerUserId, string checkerUserName);
        Task<bool> RejectPaymentAsync(int id, string checkerUserId, string checkerUserName, string rejectionReason);
        Task<int> AddPaymentForApprovalAsync(MonthlyRentPayment payment, string makerUserId, string makerUserName, MakerAction action);
        Task<bool> UpdatePaymentForApprovalAsync(MonthlyRentPayment payment, string makerUserId, string makerUserName);
        Task<bool> DeletePaymentForApprovalAsync(int id, string makerUserId, string makerUserName);
        Task<bool> HasPendingChangesAsync(int id);
    }
}