using RentManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class HraValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var employee = (Employee)validationContext.ObjectInstance;

        if (employee.HouseRentAllowance.HasValue && employee.TotalSalary.HasValue)
        {
            decimal monthlySalary = employee.TotalSalary.Value / 12;
            if (employee.HouseRentAllowance.Value > monthlySalary)
            {
                return new ValidationResult("House Rent Allowance cannot be more than one month’s salary.");
            }
        }

        return ValidationResult.Success;
    }
}
