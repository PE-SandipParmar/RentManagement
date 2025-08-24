using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class BrokeragePayment
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Lease Reference is required.")]
        public int LeaseId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Employee is required.")]
        public int EmployeeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Owner is required.")]
        public int VendorId { get; set; }

        [Required(ErrorMessage = "Payment Month is required.")]
        [DataType(DataType.Date)]
        public DateTime? PaymentMonth { get; set; }

        [Required(ErrorMessage = "Brokerage Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Brokerage Amount must be greater than 0.")]
        [Display(Name = "Brokerage Amount")]
        public decimal? BrokerageAmount { get; set; }

        [Display(Name = "TDS Applicable")]
        public int TDSApplicableId { get; set; } = 0; // 0 means No TDS

        [Range(0, 100, ErrorMessage = "TDS Rate must be between 0 and 100.")]
        [Display(Name = "TDS Rate (%)")]
        public decimal TDSRate { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "TDS Amount must be positive.")]
        [Display(Name = "TDS Amount")]
        public decimal TDSAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Net Payable Amount must be positive.")]
        [Display(Name = "Net Payable Amount")]
        public decimal NetPayableAmount { get; set; }

        [StringLength(50)]
        [Display(Name = "Payment Status")]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(50)]
        [Display(Name = "DSC Approval Status")]
        public string DSCApprovalStatus { get; set; } = "Pending";

        public int? ApprovedBy { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ApprovedDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? PaymentDate { get; set; }

        [StringLength(200)]
        public string TransactionReference { get; set; } = string.Empty;

        [StringLength(500)]
        public string Remark { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ModifiedDate { get; set; }

        public int? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties for display
        public string? EmployeeName { get; set; }
        public string? VendorName { get; set; }
        public string? LeaseName { get; set; }

        // Business rule properties (not stored in DB)
        public decimal EmployeeSalary { get; set; }
        public decimal LeaseMaxBrokerage { get; set; }
        public DateTime? LeaseEndDate { get; set; }
        
        // Computed property for maximum allowed brokerage
        public decimal MaxAllowedBrokerage => Math.Max(EmployeeSalary, LeaseMaxBrokerage);

        // Validation for business rules
        public bool IsValidBrokerageAmount =>
            BrokerageAmount.HasValue && BrokerageAmount <= MaxAllowedBrokerage;
    }

    // Supporting classes for dropdowns and API responses


    //public class ValidationResult
    //{
    //    public bool IsValid { get; set; }
    //    public string ErrorMessage { get; set; } = string.Empty;
    //}


    public class LeaseDetails
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal MaxBrokerageAmount { get; set; }
        public decimal MonthlyRent { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
    }
    public class BrokerageValidationResponse
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal EmployeeSalary { get; set; }
        public decimal LeaseMaxBrokerage { get; set; }
        public decimal MaxAllowed { get; set; }
        public bool HasExistingPayment { get; set; }
        public DateTime? ExistingPaymentDate { get; set; }
        public decimal? ExistingPaymentAmount { get; set; }
    }
    // Custom validation attribute for brokerage payment business rules
    public class BrokeragePaymentValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is BrokeragePayment payment)
            {
                // Check if brokerage amount exceeds limits
                if (payment.BrokerageAmount > payment.MaxAllowedBrokerage)
                {
                    ErrorMessage = $"Brokerage amount cannot exceed ₹{payment.MaxAllowedBrokerage:N2} " +
                                 $"(Max of Salary: ₹{payment.EmployeeSalary:N2} or Lease Max: ₹{payment.LeaseMaxBrokerage:N2})";
                    return false;
                }

                // TDS validation
                if (payment.TDSApplicableId > 0 && payment.TDSRate <= 0)
                {
                    ErrorMessage = "TDS Rate is required when TDS is applicable.";
                    return false;
                }

                return true;
            }
            return false;
        }
    }
}