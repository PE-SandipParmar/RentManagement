using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class SecurityDeposit
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,ErrorMessage = "Employee is required.")]
        public int EmployeeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vendor is required.")]
        public int VendorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Lease is required.")]
        public int LeaseId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "Approval status is required.")]
        [StringLength(20, ErrorMessage = "Approval status cannot exceed 20 characters.")]
        public string ApprovalStatus { get; set; } = "Pending";

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(50, ErrorMessage = "Modified By cannot exceed 50 characters.")]
        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public string? EmployeeName { get; set; }
        public string? VendorName { get; set; }
        public string? LeaseName { get; set; }
    }
}
