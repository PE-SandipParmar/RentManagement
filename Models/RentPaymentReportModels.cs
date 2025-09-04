using System;
using System.Collections.Generic;

namespace RentManagement.Models
{
    // Rent Payment Report Filter Model
    public class RentPaymentReportFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? VendorCode { get; set; }
        public string? VendorName { get; set; }
        public string? PAN { get; set; }
        public string? VoucherNumber { get; set; }
        public string? ChequeNumber { get; set; }
        public string? PaymentStatus { get; set; }
        public string? FinancialYear { get; set; }
        public string? Month { get; set; }
        public string? BankName { get; set; }
        public string? ProjectName { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool IncludeZeroAmounts { get; set; } = false;
        public string? SortBy { get; set; } = "PaymentDate";
        public string? SortOrder { get; set; } = "DESC";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Rent Payment Report Model
    public class RentPaymentReportModel
    {
        public string ReportTitle { get; set; } = "Rent Payment Report";
        public string OrganizationName { get; set; } = "Food Safety and Standards Authority of India";
        public string OrganizationAddress { get; set; } = "FDA Bhawan, Kotla Marg, near Bal Bhavan, New Delhi - 110002";
        public string VoucherNumber { get; set; } = string.Empty;
        public string ChequeNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentReference { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public RentPaymentReportFilterModel Filter { get; set; } = new RentPaymentReportFilterModel();
        public List<RentPaymentItem> PaymentItems { get; set; } = new List<RentPaymentItem>();
        public RentPaymentSummary Summary { get; set; } = new RentPaymentSummary();
        public int TotalRecords { get; set; }
        public string ReportType { get; set; } = "RentPayment";
        public string ReportPeriod { get; set; } = string.Empty;
    }

    // Rent Payment Item
    public class RentPaymentItem
    {
        public int SlNo { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PAN { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string POAdvanceNumber { get; set; } = string.Empty;
        public string IFSC { get; set; } = string.Empty;
        public string GSTN { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Adjustment { get; set; }
        public decimal Recoveries { get; set; }
        public decimal Payable { get; set; }
        public string ProjectCoAHead { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaseReference { get; set; } = string.Empty;
        public string PaymentType { get; set; } = "Rent";
        public string PaymentStatus { get; set; } = "Posted";
        public DateTime PaymentDate { get; set; }
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public decimal TDSAmount { get; set; }
        public decimal NetPayableAmount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    // Rent Payment Summary
    public class RentPaymentSummary
    {
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalAdjustment { get; set; }
        public decimal TotalRecoveries { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal TotalTDSAmount { get; set; }
        public decimal TotalNetPayable { get; set; }
        public int UniqueVendors { get; set; }
        public int UniqueEmployees { get; set; }
        public int UniqueBanks { get; set; }
        public Dictionary<string, decimal> AmountByMonth { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> AmountByVendor { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> AmountByEmployee { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> AmountByBank { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> AmountByProject { get; set; } = new Dictionary<string, decimal>();
    }

    // Rent Payment Report Export Options
    public class RentPaymentReportExportOptions
    {
        public string Format { get; set; } = "PDF"; // PDF, Excel, CSV
        public bool IncludeSummary { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeFilters { get; set; } = true;
        public string PaperSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";
        public bool IncludeHeader { get; set; } = true;
        public bool IncludeFooter { get; set; } = true;
        public string CompanyLogo { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ReportTitle { get; set; } = "Rent Payment Report";
        public bool IncludeBankDetails { get; set; } = true;
        public bool IncludeRemarks { get; set; } = true;
    }

    // Rent Payment Report Chart Data
    public class RentPaymentChartData
    {
        public List<RentPaymentMonthlyTrend> MonthlyTrends { get; set; } = new List<RentPaymentMonthlyTrend>();
        public List<RentPaymentVendorBreakdown> VendorBreakdown { get; set; } = new List<RentPaymentVendorBreakdown>();
        public List<RentPaymentEmployeeBreakdown> EmployeeBreakdown { get; set; } = new List<RentPaymentEmployeeBreakdown>();
        public List<RentPaymentBankBreakdown> BankBreakdown { get; set; } = new List<RentPaymentBankBreakdown>();
        public List<RentPaymentProjectBreakdown> ProjectBreakdown { get; set; } = new List<RentPaymentProjectBreakdown>();
    }

    public class RentPaymentMonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int PaymentCount { get; set; }
    }

    public class RentPaymentVendorBreakdown
    {
        public string VendorName { get; set; } = string.Empty;
        public string PAN { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RentPaymentEmployeeBreakdown
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RentPaymentBankBreakdown
    {
        public string BankName { get; set; } = string.Empty;
        public string IFSC { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RentPaymentProjectBreakdown
    {
        public string ProjectName { get; set; } = string.Empty;
        public string CoAHead { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    // Rent Payment Report Pegging Model
    public class RentPaymentPeggingModel
    {
        public int PaymentId { get; set; }
        public string PaymentType { get; set; } = "Rent";
        public string VoucherNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<RentPaymentPeggingDetail> PeggingDetails { get; set; } = new List<RentPaymentPeggingDetail>();
    }

    public class RentPaymentPeggingDetail
    {
        public int LeaseId { get; set; }
        public string LeaseRefNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal TDSAmount { get; set; }
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal NetPayableAmount { get; set; }
    }

    // Enhanced Vendor Model for Bank Details
    public class VendorBankDetails
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IFSC { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
