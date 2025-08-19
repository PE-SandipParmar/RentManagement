using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class EmployeeReport
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string AssociatedLease { get; set; }
        public decimal TotalPayment { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
