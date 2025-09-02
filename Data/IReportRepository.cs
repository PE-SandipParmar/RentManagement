using RentManagement.Models;
using System.Threading.Tasks;

namespace RentManagement.Data
{
    public interface IReportRepository
    {
        // Lease Reports
        Task<LeaseReportModel> GetLeaseReportAsync(ReportFilterModel filter);
        Task<LeaseReportModel> GetLeaseSummaryReportAsync(ReportFilterModel filter);
        Task<LeaseReportModel> GetLeaseApprovalReportAsync(ReportFilterModel filter);
        Task<LeaseReportModel> GetLeaseExpiryReportAsync(ReportFilterModel filter);

        // Payment Reports
        Task<PaymentReportModel> GetPaymentReportAsync(ReportFilterModel filter);
        Task<PaymentReportModel> GetPaymentSummaryReportAsync(ReportFilterModel filter);
        Task<PaymentReportModel> GetOverduePaymentReportAsync(ReportFilterModel filter);
        Task<PaymentReportModel> GetPaymentCollectionReportAsync(ReportFilterModel filter);

        // Employee Reports
        Task<EmployeeReportModel> GetEmployeeReportAsync(ReportFilterModel filter);
        Task<EmployeeReportModel> GetEmployeeLeaseReportAsync(ReportFilterModel filter);
        Task<EmployeeReportModel> GetEmployeeHRAReportAsync(ReportFilterModel filter);
        Task<EmployeeReportModel> GetDepartmentWiseReportAsync(ReportFilterModel filter);

        // Vendor Reports
        Task<VendorReportModel> GetVendorReportAsync(ReportFilterModel filter);
        Task<VendorReportModel> GetVendorLeaseReportAsync(ReportFilterModel filter);
        Task<VendorReportModel> GetVendorPaymentReportAsync(ReportFilterModel filter);

        // Financial Reports
        Task<FinancialReportModel> GetFinancialReportAsync(ReportFilterModel filter);
        Task<FinancialReportModel> GetMonthlyFinancialReportAsync(ReportFilterModel filter);
        Task<FinancialReportModel> GetYearlyFinancialReportAsync(ReportFilterModel filter);
        Task<FinancialReportModel> GetRevenueReportAsync(ReportFilterModel filter);

        // Approval Reports
        Task<ApprovalReportModel> GetApprovalReportAsync(ReportFilterModel filter);
        Task<ApprovalReportModel> GetApprovalSummaryReportAsync(ReportFilterModel filter);
        Task<ApprovalReportModel> GetApprovalPendingReportAsync(ReportFilterModel filter);

        // TDS Reports
        Task<TDSReportModel> GetTDSReportAsync(TDSReportFilterModel filter);
        Task<TDSReportModel> GetTDSMonthlyReportAsync(TDSReportFilterModel filter);
        Task<TDSReportModel> GetTDSVendorReportAsync(TDSReportFilterModel filter);
        Task<TDSReportModel> GetTDSEmployeeReportAsync(TDSReportFilterModel filter);
        Task<TDSChartData> GetTDSChartDataAsync(TDSReportFilterModel filter);
        Task<TDSPeggingModel> GetTDSPeggingAsync(int paymentId, string paymentType);

        // TDS Brokerage Reports
        Task<TDSBrokerageReportModel> GetTDSBrokerageReportAsync(TDSBrokerageReportFilterModel filter);
        Task<TDSBrokerageReportModel> GetTDSBrokerageMonthlyReportAsync(TDSBrokerageReportFilterModel filter);
        Task<TDSBrokerageReportModel> GetTDSBrokerageVendorReportAsync(TDSBrokerageReportFilterModel filter);
        Task<TDSBrokerageReportModel> GetTDSBrokerageEmployeeReportAsync(TDSBrokerageReportFilterModel filter);
        Task<TDSBrokerageChartData> GetTDSBrokerageChartDataAsync(TDSBrokerageReportFilterModel filter);
        Task<TDSBrokeragePeggingModel> GetTDSBrokeragePeggingAsync(int paymentId, string paymentType);

        // Dashboard Reports
        Task<DashboardReportModel> GetDashboardStatisticsAsync();
        Task<DashboardReportModel> GetDashboardStatisticsByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<List<MonthlyData>> GetMonthlyTrendsAsync(int year);
        Task<List<DepartmentData>> GetDepartmentStatisticsAsync();
        Task<List<RecentActivity>> GetRecentActivitiesAsync(int count = 10);

        // Export Methods
        Task<byte[]> ExportToPdfAsync(BaseReportModel report, string templatePath);
        Task<byte[]> ExportToExcelAsync(BaseReportModel report);
        Task<byte[]> ExportToCsvAsync(BaseReportModel report);
        Task<byte[]> ExportTDSReportToPdfAsync(TDSReportModel report, TDSReportExportOptions options);
        Task<byte[]> ExportTDSReportToExcelAsync(TDSReportModel report, TDSReportExportOptions options);
        Task<string> ExportTDSReportToCsvAsync(TDSReportModel report, TDSReportExportOptions options);
        Task<byte[]> ExportTDSBrokerageReportToPdfAsync(TDSBrokerageReportModel report, TDSBrokerageReportExportOptions options);
        Task<byte[]> ExportTDSBrokerageReportToExcelAsync(TDSBrokerageReportModel report, TDSBrokerageReportExportOptions options);
        Task<string> ExportTDSBrokerageReportToCsvAsync(TDSBrokerageReportModel report, TDSBrokerageReportExportOptions options);

        // Diagnostic Methods
        Task<object> GetTDSDiagnosticInfoAsync();
        Task<object> TestBasicTDSQueryAsync();
        Task<object> GetTDSBrokerageDiagnosticInfoAsync();
        Task<bool> TestBasicTDSBrokerageQueryAsync();

        // Utility Methods
        Task<int> GetTotalLeasesCountAsync(ReportFilterModel filter);
        Task<int> GetTotalEmployeesCountAsync(ReportFilterModel filter);
        Task<int> GetTotalVendorsCountAsync(ReportFilterModel filter);
        Task<decimal> GetTotalRentAmountAsync(ReportFilterModel filter);
        Task<decimal> GetTotalMaintenanceAmountAsync(ReportFilterModel filter);
    }
}
