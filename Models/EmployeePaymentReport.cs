using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class EmployeePaymentReport
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string VendorName { get; set; }
        public string AssociatedLease { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal TDS { get; set; }
        public decimal SecurityDepositOrBrokerage { get; set; }
        public string IFSCCode { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentType { get; set; } // Brokerage/Security Deposit
        public DateTime PaymentDate { get; set; }
    }
}
