using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public class Employee
    {
        public int? Id { get; set; }

        //[Required(ErrorMessage = "Code is required.")]
        //[StringLength(20, ErrorMessage = "Code cannot be longer than 20 characters.")]
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
        //[RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Please enter a valid PAN number.")]
        [RegularExpression(@"^(?i)[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Please enter a valid PAN number.")]

        public string? Pan{ get; set; }

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

        //[HraValidation]
        [Required(ErrorMessage = "House Rent Allowance is required.")]
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
    }
}
