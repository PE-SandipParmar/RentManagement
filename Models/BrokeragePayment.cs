using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{


    public class BrokeragePayment
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

        [Required(ErrorMessage = "Brokerage Amount is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Brokerage Amount must be positive.")]
        public decimal? BrokerageAmount { get; set; }

        
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
        public string PaymentStatus { get; set; } = string.Empty;

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

        public string? EmployeeName { get; set; }
        public string? VendorName { get; set; }
        public string? LeaseName { get; set; }
    }

}
