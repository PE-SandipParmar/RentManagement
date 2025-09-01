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

        [Display(Name = "Remarks")]
        [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters")]
        public string? Remark { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Approval Workflow Fields
        [Display(Name = "Approval Status")]
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

        [Display(Name = "Maker User ID")]
        public string? MakerUserId { get; set; }

        [Display(Name = "Maker User Name")]
        public string? MakerUserName { get; set; }

        [Display(Name = "Checker User ID")]
        public string? CheckerUserId { get; set; }

        [Display(Name = "Checker User Name")]
        public string? CheckerUserName { get; set; }

        [Display(Name = "Maker Action")]
        public MakerAction MakerAction { get; set; } = MakerAction.Create;

        [Display(Name = "Approval Date")]
        public DateTime? CheckerApprovalDate { get; set; }
        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Is Active Record")]
        public bool IsActiveRecord { get; set; } = true;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }
        public int? CreatedById { get; set; }
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Modified By")]
        public string? ModifiedBy { get; set; }
        public int? ModifiedById { get; set; }

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

        // Helper property to get approval status display text
        public string ApprovalStatusText => ApprovalStatus switch
        {
            ApprovalStatus.Pending => "Pending Approval",
            ApprovalStatus.Approved => "Approved",
            ApprovalStatus.Rejected => "Rejected",
            _ => "Unknown"
        };

        // Helper property to check if record is visible in main list
        public bool IsVisibleInMainList => ApprovalStatus == ApprovalStatus.Approved && IsActiveRecord;
    }

    public class SecurityDepositListViewModel
    {
        public List<SecurityDeposit> SecurityDeposits { get; set; } = new List<SecurityDeposit>();
        public List<SecurityDeposit> PendingApprovals { get; set; } = new List<SecurityDeposit>();
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public string ApprovalStatusFilter { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public UserRole CurrentUserRole { get; set; }
        public bool ShowApprovalSection { get; set; }
    }

    // Request models for AJAX operations
    public class SecurityDepositCreateRequest
    {
        public int EmployeeId { get; set; }
        public int VendorId { get; set; }
        public int LeaseId { get; set; }
        public decimal Amount { get; set; }
        public decimal? TdsRate { get; set; }
        public decimal? TdsAmount { get; set; }
        public string? Remark { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class SecurityDepositUpdateRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int VendorId { get; set; }
        public int LeaseId { get; set; }
        public decimal Amount { get; set; }
        public decimal? TdsRate { get; set; }
        public decimal? TdsAmount { get; set; }
        public string? Remark { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class SecurityDepositRejectionRequest
    {
        public int Id { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}