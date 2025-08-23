namespace RentManagement.Models
{
    public class LeaseName
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public int VendorId { get; set; }
        public decimal MaxBrokerageAmount { get; set; }
    }
}
