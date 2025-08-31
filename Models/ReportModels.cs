using System;
using System.Collections.Generic;

namespace RentManagement.Models
{
    // Base report model
    public abstract class BaseReportModel
    {
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalRecords { get; set; }
    }

    // Lease Reports
    public class LeaseReportModel : BaseReportModel
    {
        public List<LeaseReportItem> Leases { get; set; } = new List<LeaseReportItem>();
        public decimal TotalMonthlyRent { get; set; }
        public decimal TotalRentAmount { get; set; }
        public decimal TotalMaintenancePayment { get; set; }
        public int ActiveLeases { get; set; }
        public int ExpiredLeases { get; set; }
        public int PendingApprovals { get; set; }
    }

    public class LeaseReportItem
    {
        public int Id { get; set; }
        public string RefNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string LeaseTypeName { get; set; } = string.Empty;
        public decimal? MonthlyRentPayable { get; set; }
        public decimal? RentAmount { get; set; }
        public decimal? MaintenancePayment { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MakerUserName { get; set; } = string.Empty;
        public string CheckerUserName { get; set; } = string.Empty;
    }

    // Payment Reports
    public class PaymentReportModel : BaseReportModel
    {
        public List<PaymentReportItem> Payments { get; set; } = new List<PaymentReportItem>();
        public decimal TotalAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public int TotalPayments { get; set; }
        public int PaidPayments { get; set; }
        public int PendingPayments { get; set; }
    }

    public class PaymentReportItem
    {
        public int Id { get; set; }
        public string LeaseRefNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    // Employee Reports
    public class EmployeeReportModel : BaseReportModel
    {
        public List<EmployeeReportItem> Employees { get; set; } = new List<EmployeeReportItem>();
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public decimal TotalHRA { get; set; }
    }

    public class EmployeeReportItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public decimal? HRA { get; set; }
        public int ActiveLeases { get; set; }
        public decimal TotalRentAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Vendor/Owner Reports
    public class VendorReportModel : BaseReportModel
    {
        public List<VendorReportItem> Vendors { get; set; } = new List<VendorReportItem>();
        public int TotalVendors { get; set; }
        public int ActiveVendors { get; set; }
        public decimal TotalRentAmount { get; set; }
        public decimal TotalMaintenanceAmount { get; set; }
    }

    public class VendorReportItem
    {
        public int Id { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ActiveLeases { get; set; }
        public decimal TotalRentAmount { get; set; }
        public decimal TotalMaintenanceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Financial Reports
    public class FinancialReportModel : BaseReportModel
    {
        public List<FinancialReportItem> FinancialData { get; set; } = new List<FinancialReportItem>();
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalOverdueAmount { get; set; }
    }

    public class FinancialReportItem
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal RentRevenue { get; set; }
        public decimal MaintenanceRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalLeases { get; set; }
        public int ActiveLeases { get; set; }
    }

    // Approval Reports
    public class ApprovalReportModel : BaseReportModel
    {
        public List<ApprovalReportItem> Approvals { get; set; } = new List<ApprovalReportItem>();
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal TotalRejectedAmount { get; set; }
    }

    public class ApprovalReportItem
    {
        public int Id { get; set; }
        public string RefNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string MakerUserName { get; set; } = string.Empty;
        public string CheckerUserName { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public string MakerAction { get; set; } = string.Empty;
    }

    // Report Filter Models
    public class ReportFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? EmployeeId { get; set; }
        public int? VendorId { get; set; }
        public int? LeaseTypeId { get; set; }
        public string? Status { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? SearchTerm { get; set; }
        public string ReportFormat { get; set; } = "PDF"; // PDF, Excel, CSV
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
    }

    // Dashboard Statistics
    public class DashboardReportModel
    {
        public int TotalLeases { get; set; }
        public int ActiveLeases { get; set; }
        public int ExpiredLeases { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalVendors { get; set; }
        public decimal TotalMonthlyRent { get; set; }
        public decimal TotalMaintenancePayment { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalOverdueAmount { get; set; }
        public List<MonthlyData> MonthlyTrends { get; set; } = new List<MonthlyData>();
        public List<DepartmentData> DepartmentStats { get; set; } = new List<DepartmentData>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }

    public class MonthlyData
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int NewLeases { get; set; }
        public decimal TotalRent { get; set; }
        public int Approvals { get; set; }
    }

    public class DepartmentData
    {
        public string Department { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public int ActiveLeases { get; set; }
        public decimal TotalRentAmount { get; set; }
    }

    //public class RecentActivity
    //{
    //    public int Id { get; set; }
    //    public string ActivityType { get; set; } = string.Empty;
    //    public string Description { get; set; } = string.Empty;
    //    public string UserName { get; set; } = string.Empty;
    //    public DateTime Timestamp { get; set; }
    //    public string Status { get; set; } = string.Empty;
    //}
}
