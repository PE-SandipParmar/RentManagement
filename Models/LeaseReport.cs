using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class LeaseReport
    {
        public string LeaseName { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime LeaseEndDate { get; set; }
        public string EmployeeName { get; set; }
        public string VendorName { get; set; }
        public decimal SecurityDepositAmount { get; set; }
        public decimal BrokerageAmount { get; set; }
        public string Department { get; set; }
    }
}
