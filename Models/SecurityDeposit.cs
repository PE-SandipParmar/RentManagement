using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class SecurityDeposit
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an employee")]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Please select a vendor/owner")]
        [Display(Name = "Vendor/Owner")]
        public int VendorId { get; set; }

        [Required(ErrorMessage = "Please select a lease")]
        [Display(Name = "Lease")]
        public int LeaseId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [Display(Name = "Deposit Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Display(Name = "TDS Rate (%)")]
        [Range(0, 100, ErrorMessage = "TDS Rate must be between 0 and 100%")]
        [DataType(DataType.Text)]
        public decimal? TdsRate { get; set; }

        [Display(Name = "TDS Amount")]
        [DataType(DataType.Currency)]
        public decimal? TdsAmount { get; set; }

        [Required(ErrorMessage = "Approval status is required")]
        [Display(Name = "Approval Status")]
        public string ApprovalStatus { get; set; } = "Pending";

        [Display(Name = "Remarks")]
        [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters")]
        public string? Remark { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Modified By")]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        // Navigation/Display Properties
        public string? EmployeeName { get; set; }
        public string? VendorName { get; set; }
        public string? LeaseName { get; set; }

        // Calculated Properties
        public decimal NetAmount
        {
            get
            {
                return Amount - (TdsAmount ?? 0);
            }
        }

        public string DisplayStatus
        {
            get
            {
                return IsActive ? "Active" : "Inactive";
            }
        }

        public string FormattedAmount
        {
            get
            {
                return Amount.ToString("C");
            }
        }

        public string FormattedTdsAmount
        {
            get
            {
                return TdsAmount?.ToString("C") ?? "-";
            }
        }

        public string FormattedNetAmount
        {
            get
            {
                return NetAmount.ToString("C");
            }
        }

        public string FormattedTdsRate
        {
            get
            {
                return TdsRate?.ToString("F2") + "%" ?? "-";
            }
        }
    }
}