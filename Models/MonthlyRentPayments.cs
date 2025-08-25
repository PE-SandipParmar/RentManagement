using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{


    public class MonthlyRentPayment
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Lease Ref. is required.")]
        public int LeaseId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Employee is required.")]
        public int EmployeeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vendor is required.")]
        public int VendorId { get; set; }

        [Required(ErrorMessage = "Payment Month is required.")]
        [DataType(DataType.Date)]
        public DateTime? PaymentMonth { get; set; }

        [Required(ErrorMessage = "Monthly Lease Amount is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Monthly Lease Amount must be positive.")]
        public decimal? MonthlyLeaseAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TDS Applicable is required.")]
        public int TDSApplicableId { get; set; }

        [Range(0, 100, ErrorMessage = "TDS Rate must be between 0 and 100.")]
        public decimal TDSRate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "TDS Amount must be positive.")]
        public decimal TDSAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Net Payable Amount must be positive.")]
        public decimal NetPayableAmount { get; set; }

        [Required(ErrorMessage = "Lease End Date is required.")]
        [DataType(DataType.Date)]
        public DateTime LeaseEndDate { get; set; }

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(50)]
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

        // Navigation properties
        public string? EmployeeName { get; set; }
        public string? VendorName { get; set; }
        public string? LeaseName { get; set; }
        public string? TDSApplicableName { get; set; }

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

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Is Active Record")]
        public bool IsActiveRecord { get; set; } = true;

        // Helper property to check if payment is visible in main list
        public bool IsVisibleInMainList => ApprovalStatus == ApprovalStatus.Approved && IsActiveRecord;

        // Helper property to get approval status display text
        public string ApprovalStatusText => ApprovalStatus switch
        {
            ApprovalStatus.Pending => "Pending Approval",
            ApprovalStatus.Approved => "Approved",
            ApprovalStatus.Rejected => "Rejected",
            _ => "Unknown"
        };
    }

    public class MonthlyPaymentListViewModel
    {
        public List<MonthlyRentPayment> Payments { get; set; } = new List<MonthlyRentPayment>();
        public List<MonthlyRentPayment> PendingApprovals { get; set; } = new List<MonthlyRentPayment>();
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

}
