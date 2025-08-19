using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IMISReportRepository
    {
        Task<IEnumerable<EmployeeReport>> GetEmployeeReportAsync(EmployeeReportFilter filter);
        Task<IEnumerable<LeaseReport>> GetLeaseReportAsync(LeaseReportFilter filter);
        Task<IEnumerable<EmployeePaymentReport>> GetEmployeePaymentReportAsync(EmployeePaymentReportFilter filter);
    }
}
