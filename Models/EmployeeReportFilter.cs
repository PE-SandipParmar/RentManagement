using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class EmployeeReportFilter
    {
        public string FinancialYear { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string FileFormat { get; set; }
    }
}
