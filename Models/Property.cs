using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class Property
    {
        public int Id { get; set; }

        [Display(Name = "Property Code")]
        public string PropertyCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Owner is required")]
        [Display(Name = "Owner")]
        public int VendorId { get; set; }

        [Display(Name = "Owner Code")]
        public string? VendorCode { get; set; }

        [Display(Name = "Owner Name")]
        public string? VendorName { get; set; }

        [Required(ErrorMessage = "Property Address is required")]
        [Display(Name = "Property Address")]
        [StringLength(500, ErrorMessage = "Property Address cannot exceed 500 characters")]
        public string PropertyAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Total Rent Amount is required")]
        [Display(Name = "Total Rent Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total Rent Amount must be greater than 0")]
        public decimal TotalRentAmount { get; set; }

        [Display(Name = "Linked Employees")]
        public string? LinkedEmployees { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

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
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Is Active Record")]
        public bool IsActiveRecord { get; set; } = true;

        // Helper property to get linked employees as a list
        public List<string> LinkedEmployeesList
        {
            get
            {
                if (string.IsNullOrEmpty(LinkedEmployees))
                    return new List<string>();
                return LinkedEmployees.Split(',').Select(x => x.Trim()).ToList();
            }
            set
            {
                LinkedEmployees = string.Join(",", value);
            }
        }

        // Helper property to check if property is visible in main list
        public bool IsVisibleInMainList => ApprovalStatus == ApprovalStatus.Approved && IsActiveRecord;

        // Helper property to get approval status display text
        public string ApprovalStatusText => ApprovalStatus switch
        {
            ApprovalStatus.Pending => "Pending Approval",
            ApprovalStatus.Approved => "Approved",
            ApprovalStatus.Rejected => "Rejected",
            _ => "Unknown"
        };

        // Helper property to get maker action display text
        public string MakerActionText => MakerAction switch
        {
            MakerAction.Create => "Create",
            MakerAction.Update => "Update",
            MakerAction.Delete => "Delete",
            _ => "Unknown"
        };
    }

    public class PropertyViewModel
    {
        public Property Property { get; set; } = new Property();
        public List<Vendor> AvailableVendors { get; set; } = new List<Vendor>();
        public List<Employee> AvailableEmployees { get; set; } = new List<Employee>();
        public List<string> SelectedEmployees { get; set; } = new List<string>();
    }

    public class PropertyListViewModel
    {
        public List<Property> Properties { get; set; } = new List<Property>();
        public List<Property> PendingApprovals { get; set; } = new List<Property>();
        public List<Vendor> Vendors { get; set; } = new List<Vendor>();
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public string ApprovalStatusFilter { get; set; } = string.Empty;
        public int? VendorFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public UserRole CurrentUserRole { get; set; }
        public bool ShowApprovalSection { get; set; }
    }
}
