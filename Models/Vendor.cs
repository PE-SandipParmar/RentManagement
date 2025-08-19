using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class Vendor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vendor Code is required")]
        [Display(Name = "Vendor Code")]
        public string VendorCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vendor Name is required")]
        [Display(Name = "Vendor Name")]
        [StringLength(200, ErrorMessage = "Vendor Name cannot exceed 200 characters")]
        public string VendorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "PAN Number is required")]
        [Display(Name = "PAN Number")]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Invalid PAN Number format")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "PAN Number must be 10 characters")]
        public string PANNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required")]
        [Display(Name = "Mobile Number")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile Number must be 10 digits")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Mobile Number must be 10 digits")]
        public string MobileNumber { get; set; } = string.Empty;

        [Display(Name = "Alternate Number")]
        //[RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Alternate Number must be 10 digits")]
        //[StringLength(10, MinimumLength = 10, ErrorMessage = "Alternate Number must be 10 digits")]
        public string? AlternateNumber { get; set; }

        [Required(ErrorMessage = "Email ID is required")]
        [Display(Name = "Email ID")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string EmailId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Holder Name is required")]
        [Display(Name = "Account Holder Name")]
        [StringLength(200, ErrorMessage = "Account Holder Name cannot exceed 200 characters")]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bank Name is required")]
        [Display(Name = "Bank Name")]
        [StringLength(200, ErrorMessage = "Bank Name cannot exceed 200 characters")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Branch Name is required")]
        [Display(Name = "Branch Name")]
        [StringLength(200, ErrorMessage = "Branch Name cannot exceed 200 characters")]
        public string BranchName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Number is required")]
        [Display(Name = "Account Number")]
        [StringLength(50, ErrorMessage = "Account Number cannot exceed 50 characters")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "IFSC Code is required")]
        [Display(Name = "IFSC Code")]
        //[RegularExpression(@"^[A-Z]{4}[0-9]{7}$", ErrorMessage = "Invalid IFSC Code format")]
       // [StringLength(11, MinimumLength = 11, ErrorMessage = "IFSC Code must be 11 characters")]
        public string IFSCCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Property Address is required")]
        [Display(Name = "Property Address")]
        [StringLength(500, ErrorMessage = "Property Address cannot exceed 500 characters")]
        public string PropertyAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Total Rent Amount is required")]
        [Display(Name = "Total Rent Amount")]
        [Range(0.01, 999999999.99, ErrorMessage = "Total Rent Amount must be greater than 0")]
        public decimal TotalRentAmount { get; set; }

        [Display(Name = "Linked Employees")]
        public string? LinkedEmployees { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

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

        public class VendorViewModel
        {
            public Vendor Vendor { get; set; } = new Vendor();
            public List<Employee> AvailableEmployees { get; set; } = new List<Employee>();
            public List<string> SelectedEmployees { get; set; } = new List<string>();
        }

        public class VendorListViewModel
        {
            public List<Vendor> Vendors { get; set; } = new List<Vendor>();
            public string SearchTerm { get; set; } = string.Empty;
            public string StatusFilter { get; set; } = string.Empty;
            public int CurrentPage { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public int TotalRecords { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        }
    }
}
