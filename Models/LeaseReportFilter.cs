using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class LeaseReportFilter
    {
        public string FinancialYear { get; set; }
        public DateTime? LeaseStartDate { get; set; }
        public DateTime? LeaseEndDate { get; set; }
        public string EmployeeName { get; set; }
        public string VendorName { get; set; }
        public string FileFormat { get; set; }
    }
}
