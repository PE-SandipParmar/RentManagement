using System;
using System.Collections.Generic;

namespace RentManagement.Models
{
    // TDS Report Filter Model
    public class TDSReportFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? VendorCode { get; set; }
        public string? VendorName { get; set; }
        public string? PAN { get; set; }
        public int? TDSApplicableId { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentType { get; set; } // MonthlyRent, Brokerage, SecurityDeposit
        public string? FinancialYear { get; set; }
        public string? Month { get; set; }
        public string? VoucherNumber { get; set; }
        public string? ProjectName { get; set; }
        public string? CoAName { get; set; }
        public string? ExportFormat { get; set; } // PDF, Excel, CSV
        public bool IncludeZeroTDS { get; set; } = false;
        public bool IncludeExemptions { get; set; } = true;
        public string? SortBy { get; set; } = "Date";
        public string? SortOrder { get; set; } = "DESC";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // TDS Report Model
    public class TDSReportModel
    {
        public string ReportTitle { get; set; } = "TDS Report - Lease Payable";
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public TDSReportFilterModel Filter { get; set; } = new TDSReportFilterModel();
        public List<TDSReportItem> TDSItems { get; set; } = new List<TDSReportItem>();
        public TDSReportSummary Summary { get; set; } = new TDSReportSummary();
        public int TotalRecords { get; set; }
        public string ReportType { get; set; } = "TDS";
    }

    // TDS Report Item
    public class TDSReportItem
    {
        public int SlNo { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string PAN { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal TDSDeductedOn { get; set; } // Gross amount
        public decimal TDSRate { get; set; }
        public decimal TDSAmount { get; set; }
        public decimal GrossValue { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string CoAName { get; set; } = string.Empty;
        public string Narration { get; set; } = string.Empty;
        public string ExemptionCertificateNo { get; set; } = string.Empty;
        public DateTime? ExemptionFromDate { get; set; }
        public DateTime? ExemptionToDate { get; set; }
        public string PaymentType { get; set; } = string.Empty; // MonthlyRent, Brokerage, SecurityDeposit
        public string PaymentStatus { get; set; } = string.Empty;
        public string LeaseRefNo { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string TDSApplicableName { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // TDS Report Summary
    public class TDSReportSummary
    {
        public int TotalPayments { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalTDSAmount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal AverageTDSRate { get; set; }
        public int UniqueEmployees { get; set; }
        public int UniqueVendors { get; set; }
        public int UniquePANs { get; set; }
        public Dictionary<string, decimal> TDSByMonth { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> TDSByEmployee { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> TDSByVendor { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> TDSByPaymentType { get; set; } = new Dictionary<string, decimal>();
    }

    // TDS Report Export Options
    public class TDSReportExportOptions
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
        public string ReportTitle { get; set; } = "TDS Report - Lease Payable";
    }

    // TDS Report Chart Data
    public class TDSChartData
    {
        public List<TDSMonthlyTrend> MonthlyTrends { get; set; } = new List<TDSMonthlyTrend>();
        public List<TDSVendorBreakdown> VendorBreakdown { get; set; } = new List<TDSVendorBreakdown>();
        public List<TDSEmployeeBreakdown> EmployeeBreakdown { get; set; } = new List<TDSEmployeeBreakdown>();
        public List<TDSPaymentTypeBreakdown> PaymentTypeBreakdown { get; set; } = new List<TDSPaymentTypeBreakdown>();
    }

    public class TDSMonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public decimal TotalTDS { get; set; }
        public decimal TotalGross { get; set; }
        public int PaymentCount { get; set; }
    }

    public class TDSVendorBreakdown
    {
        public string VendorName { get; set; } = string.Empty;
        public string PAN { get; set; } = string.Empty;
        public decimal TotalTDS { get; set; }
        public decimal TotalGross { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TDSEmployeeBreakdown
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public decimal TotalTDS { get; set; }
        public decimal TotalGross { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TDSPaymentTypeBreakdown
    {
        public string PaymentType { get; set; } = string.Empty;
        public decimal TotalTDS { get; set; }
        public decimal TotalGross { get; set; }
        public int PaymentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    // TDS Report Pegging Model
    public class TDSPeggingModel
    {
        public int PaymentId { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string VoucherNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<TDSPeggingDetail> PeggingDetails { get; set; } = new List<TDSPeggingDetail>();
    }

    public class TDSPeggingDetail
    {
        public int LeaseId { get; set; }
        public string LeaseRefNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal TDSAmount { get; set; }
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        // Added missing properties to fix CS1061 errors
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
