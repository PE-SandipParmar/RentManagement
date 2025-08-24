using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class Employee
    {
        public int? Id { get; set; }
        public string? Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [RegularExpression("Male|Female|Other", ErrorMessage = "Gender must be Male, Female or Other.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aadhar number is required.")]
        [Aadhar(ErrorMessage = "Please enter a valid 12-digit Aadhar number.")]
        public string? Aadhar { get; set; }

        [Required(ErrorMessage = "PAN is required.")]
        [RegularExpression(@"^(?i)[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Please enter a valid PAN number.")]
        public string? Pan { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Department is required.")]
        public int DepartmentId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Designation is required.")]
        public int DesignationId { get; set; }

        [Required(ErrorMessage = "Date of Joining is required.")]
        [DataType(DataType.Date)]
        public DateTime? DateOfJoining { get; set; }

        public bool EligibleForLease { get; set; } = false;

        [Required(ErrorMessage = "Total Salary is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Total Salary must be a positive number.")]
        public decimal? TotalSalary { get; set; }

        [Required(ErrorMessage = "Basic Salary is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Basic Salary must be a positive number.")]
        public decimal? BasicSalary { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "House Rent Allowance must be a positive number.")]
        public decimal? HouseRentAllowance { get; set; }

        public decimal? TravelAllowance { get; set; }
        public decimal? MedicalAllowance { get; set; }
        public decimal? OtherAllowance { get; set; }
        public decimal? GrossSalaryAfterDeductions { get; set; }
        public decimal? PF { get; set; }
        public decimal? ProfessionalTax { get; set; }
        public decimal? ESI { get; set; }

        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

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

        // Helper property to check if employee is visible in main list
        public bool IsVisibleInMainList => ApprovalStatus == ApprovalStatus.Approved && IsActiveRecord;

        [Display(Name = "HRA")]
        public decimal? HRA { get; set; }

        // Helper property to get approval status display text
        public string ApprovalStatusText => ApprovalStatus switch
        {
            ApprovalStatus.Pending => "Pending Approval",
            ApprovalStatus.Approved => "Approved",
            ApprovalStatus.Rejected => "Rejected",
            _ => "Unknown"
        };
    }

    // PagedResult class for Employee with approval workflow support
    public class EmployeeListViewModel
    {
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<Employee> PendingApprovals { get; set; } = new List<Employee>();
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
    public class EmployeeRejectionRequest
    {
        public int Id { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}