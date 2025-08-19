using System.ComponentModel.DataAnnotations;

namespace RentManagement.Models
{
    public enum UserRole
    {
        [Display(Name = "Employee")]
        Employee = 1,

        [Display(Name = "Vendor")]
        Vendor = 2,

        [Display(Name = "Admin")]
        Admin = 3
    }

    public static class UserRoleExtensions
    {
        public static string GetDisplayName(this UserRole role)
        {
            return role switch
            {
                UserRole.Employee => "Employee",
                UserRole.Vendor => "Vendor",
                UserRole.Admin => "Admin",
                _ => "Unknown"
            };
        }

        public static List<UserRole> GetSelectableRoles(UserRole currentUserRole)
        {
            return currentUserRole switch
            {
                UserRole.Admin => new List<UserRole> { UserRole.Employee, UserRole.Vendor, UserRole.Admin },
                UserRole.Employee => new List<UserRole> { UserRole.Employee },
                UserRole.Vendor => new List<UserRole> { UserRole.Vendor },
                _ => new List<UserRole> { UserRole.Employee }
            };
        }
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
        public const string Vendor = "Vendor";

        public const string AdminOrEmployee = Admin + "," + Employee;
        public const string AdminOrVendor = Admin + "," + Vendor;
        public const string All = Admin + "," + Employee + "," + Vendor;
    }
}
