using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class AadharAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var aadhar = value as string;

        if (string.IsNullOrEmpty(aadhar))
        {
            return ValidationResult.Success; // use [Required] separately if needed
        }

        // Regex for exactly 12 digits
        var regex = new Regex(@"^\d{12}$");

        if (!regex.IsMatch(aadhar))
        {
            return new ValidationResult("Aadhar number must be exactly 12 digits.");
        }

        return ValidationResult.Success;
    }
}
