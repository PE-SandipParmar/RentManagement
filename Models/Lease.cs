using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class Lease
    {
        public int Id { get; set; }

        //[Required(ErrorMessage = "Perquisite Type is required.")]
        //[RegularExpression("Non-Government", ErrorMessage = "Perquisite Type must be 'Non-Government'.")]
        public string PerquisiteType { get; set; } = "Non-Government"; // Radio button, fixed value

        public string Status { get; set; } = string.Empty; // Auto fetched

        [Range(1, int.MaxValue, ErrorMessage = "Lease Type is required.")]
        public int LeaseTypeId { get; set; }

        [Required(ErrorMessage = "Lease Reference Number is required.")]
        public string RefNo { get; set; } = string.Empty; 

        [Range(1, int.MaxValue, ErrorMessage = "Lease Name is required.")]
        public int EmployeeId { get; set; }

        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Lease Reference Date is required.")]
        public DateTime? RefDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "% of Perquisite Applicable is required.")]
        public int PerquisiteApplicablePercentId { get; set; } // Dropdown id

        [Range(1, int.MaxValue, ErrorMessage = "Owner Name is required.")]
        public int VendorId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Monthly Rent Payable must be a positive number.")]
        [Required(ErrorMessage = "Monthly Rent Payable is required.")]
        public decimal? MonthlyRentPayable { get; set; }

        [Required(ErrorMessage = "Lease From Date is required.")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Required(ErrorMessage = "Lease End Date is required.")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
        [Range(1, double.MaxValue, ErrorMessage = "Rent Recovery Element.")]

        public int? RentRecoveryElementId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Rent Deposit must be a positive number.")]
        [Required(ErrorMessage = "Rent Deposit is required.")]
        public decimal? RentDeposit { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Additional Rent Recovery must be a positive number.")]
        [Required(ErrorMessage = "Additional Rent Recovery is required.")]
        public decimal? AdditionalRentRecovery { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Brokerage Amount must be a positive number.")]
        [Required(ErrorMessage = "Brokerage Amount is required.")]
        public decimal? BrokerageAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "License Fee Recovery Element is required.")]
        public int? LicenseFeeRecoveryElementId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Stamp Duty must be a positive number.")]
        [Required(ErrorMessage = "Stamp Duty is required.")]
        public decimal? StampDuty { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "License Fee Amount must be a positive number.")]
        [Required(ErrorMessage = "License Fee Amount is required.")]
        public decimal? LicenseFeeAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Payment Term is required.")]
        public int PaymentTermId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Payable On or Before is required.")]
        public int PayableOnOrBeforeId { get; set; }

        [Required(ErrorMessage = "Narration is required.")]
        [StringLength(200, ErrorMessage = "Narration cannot be longer than 200 characters.")]
        public string Narration { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }

        // Optional navigation/display properties
        
        public string? LeaseTypeName { get; set; }
        public string? PerquisiteApplicablePercent { get; set; }
        public string? EmployeeName { get; set; }
        public string? VendorName { get; set; }
        public string? RentRecoveryElementName { get; set; }
        public string? LicenseFeeRecoveryElementName { get; set; }
        public string? PaymentTermName { get; set; }
        public string? PayableOnOrBeforeName { get; set; }
        public string? TotalLeaseAmount { get; set; }

        
    }
}
