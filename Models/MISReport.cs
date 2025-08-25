namespace RentManagement.Models
{
    // Models/Employee.cs
    using System.ComponentModel.DataAnnotations;

    namespace RentPaymentSystem.Models
    {
        public class EmployeeReports
        {
            public int Id { get; set; }

            [Required]
            [StringLength(20)]
            public string Code { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; }

            [Required]
            [StringLength(50)]
            public string Department { get; set; }

            [Required]
            [StringLength(50)]
            public string Designation { get; set; }

            public bool EligibleForLease { get; set; }

            [Required]
            public decimal TotalSalary { get; set; }

            public DateTime CreatedDate { get; set; }
            public DateTime? ModifiedDate { get; set; }
            public bool IsActive { get; set; }
        }
        public class LeaseReports
        {
            public int Id { get; set; }

            [Required]
            [StringLength(20)]
            public string RefNo { get; set; }

            [Required]
            public int EmployeeId { get; set; }

            [Required]
            public int VendorId { get; set; }

            [Required]
            public decimal MonthlyRent { get; set; }

            public decimal? SecurityDeposit { get; set; }

            [Required]
            [StringLength(20)]
            public string Status { get; set; } // Active, Pending, Expired, Terminated

            [Required]
            public DateTime FromDate { get; set; }

            [Required]
            public DateTime EndDate { get; set; }

            public DateTime CreatedDate { get; set; }
            public DateTime? ModifiedDate { get; set; }

            // Navigation properties
            public virtual EmployeeReports Employee { get; set; }
            public virtual VendorReports Vendor { get; set; }
        }
        public class VendorReports
        {
            public int Id { get; set; }

            [Required]
            [StringLength(20)]
            public string VendorCode { get; set; }

            [Required]
            [StringLength(100)]
            public string VendorName { get; set; }

            [Required]
            [StringLength(10)]
            public string PanNumber { get; set; }

            [StringLength(200)]
            public string Address { get; set; }

            [StringLength(15)]
            public string ContactNumber { get; set; }

            [StringLength(100)]
            public string Email { get; set; }

            public decimal TotalRentAmount { get; set; }

            [Required]
            [StringLength(20)]
            public string Status { get; set; } // Active, Inactive

            public DateTime CreatedDate { get; set; }
            public DateTime? ModifiedDate { get; set; }

            // Navigation properties
            public virtual ICollection<Lease> Leases { get; set; }
        }
        public class ReportRequestDto
        {
            public string ReportType { get; set; } // employee, lease, vendor, financial, comprehensive
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string ExportFormat { get; set; } // html, pdf, excel, csv
            public bool ActiveOnly { get; set; }
            public bool IncludeFinancials { get; set; }
            public string Department { get; set; }
            public string Status { get; set; }
        }
        public class ReportResponseDto<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<T> Data { get; set; }
            public ReportMetadata Metadata { get; set; }
        }

        public class ReportMetadata
        {
            public int TotalRecords { get; set; }
            public DateTime GeneratedOn { get; set; }
            public string ReportType { get; set; }
            public DateRange DateRange { get; set; }
        }

        public class DateRange
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }
        public class DashboardStatsDto
        {
            public int TotalEmployees { get; set; }
            public int ActiveLeases { get; set; }
            public int TotalVendors { get; set; }
            public decimal MonthlyRent { get; set; }
            public int EligibleEmployees { get; set; }
            public int ExpiredLeases { get; set; }
            public int PendingLeases { get; set; }
            public decimal AnnualRent { get; set; }
        }
        public class FinancialSummaryDto
        {
            public decimal TotalMonthlyRent { get; set; }
            public decimal TotalPayroll { get; set; }
            public decimal TotalSecurityDeposit { get; set; }
            public int ActiveContracts { get; set; }
            public decimal AverageRentPerEmployee { get; set; }
            public List<MonthlyRentTrend> RentTrends { get; set; }
            public List<DepartmentWiseExpense> DepartmentExpenses { get; set; }
        }

        public class MonthlyRentTrend
        {
            public string Month { get; set; }
            public decimal MonthlyRent { get; set; }
            public decimal SecurityDeposit { get; set; }
        }

        public class DepartmentWiseExpense
        {
            public string Department { get; set; }
            public decimal TotalRent { get; set; }
            public int EmployeeCount { get; set; }
        }
        public class ExportRequestDto
        {
            public string ReportType { get; set; }
            public object Data { get; set; }
            public string FileName { get; set; }
            public ExportOptions Options { get; set; }
        }

        public class ExportOptions
        {
            public bool IncludeHeader { get; set; } = true;
            public bool IncludeTimestamp { get; set; } = true;
            public string DateFormat { get; set; } = "yyyy-MM-dd";
            public string NumberFormat { get; set; } = "#,##0.00";
        }
    }

}