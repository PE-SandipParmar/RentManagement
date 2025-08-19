using System;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{

    public class EmployeePaymentReportFilter
    {
        public string EmployeeName { get; set; }
        public string PaymentType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string FileFormat { get; set; }
    }
}
