using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace RentManagement.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        // GET: Employee Index with pagination and search
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var employees = await _employeeRepository.GetEmployeesAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            // Load departments and designations for the slide-over drawers
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();

            return View(employees);
        }

        // POST: Toggle employee active status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _employeeRepository.ToggleActiveStatus(id);
                TempData["SuccessMessage"] = "Employee status updated successfully!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling status: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to update employee status.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Get employee data for AJAX requests (Edit and Details drawers)
        [HttpGet]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found" });
                }

                return Json(new
                {
                    success = true,
                    employee = new
                    {
                        id = employee.Id,
                        code = employee.Code,
                        name = employee.Name,
                        dateOfBirth = employee.DateOfBirth?.ToString("yyyy-MM-dd"),
                        gender = employee.Gender,
                        email = employee.Email,
                        aadhar = employee.Aadhar,
                        pan = employee.Pan,
                        departmentId = employee.DepartmentId,
                        departmentName = employee.DepartmentName,
                        designationId = employee.DesignationId,
                        designationName = employee.DesignationName,
                        dateOfJoining = employee.DateOfJoining?.ToString("yyyy-MM-dd"),
                        eligibleForLease = employee.EligibleForLease,
                        totalSalary = employee.TotalSalary,
                        basicSalary = employee.BasicSalary,
                        houseRentAllowance = employee.HouseRentAllowance,
                        travelAllowance = employee.TravelAllowance,
                        medicalAllowance = employee.MedicalAllowance,
                        otherAllowance = employee.OtherAllowance,
                        pf = employee.PF,
                        professionalTax = employee.ProfessionalTax,
                        esi = employee.ESI,
                        grossSalaryAfterDeductions = employee.GrossSalaryAfterDeductions,
                        isActive = employee.IsActive,
                        createdAt = employee.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting employee: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while retrieving employee data." });
            }
        }

        // POST: Create new employee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            try
            {
                // Set default values for nullable fields
                employee.TravelAllowance ??= 0;
                employee.MedicalAllowance ??= 0;
                employee.OtherAllowance ??= 0;
                employee.PF ??= 0;
                employee.ProfessionalTax ??= 0;
                employee.ESI ??= 0;
                employee.HouseRentAllowance ??= 0;

                // Auto-calculate HRA if not provided or zero
                //if (!employee.HouseRentAllowance.HasValue || employee.HouseRentAllowance <= 0)
                //{
                //    if (employee.BasicSalary.HasValue && employee.BasicSalary > 0)
                //    {
                //        // HRA calculation: (Basic Salary × 50%) × 2, max = Basic Salary
                //        var maxHRA = (employee.BasicSalary.Value * 0.5m) * 2;
                //        employee.HouseRentAllowance = Math.Min(maxHRA, employee.BasicSalary.Value);
                //    }
                //    else
                //    {
                //        employee.HouseRentAllowance = 0;
                //    }
                //}

                // Calculate gross salary after deductions
                var totalDeductions = employee.PF.Value + employee.ProfessionalTax.Value + employee.ESI.Value;
                employee.GrossSalaryAfterDeductions = (employee.TotalSalary ?? 0) - totalDeductions;

                // Validate business rules
                await ValidateEmployeeBusinessRules(employee);

                // Check for duplicate email
                if (await _employeeRepository.EmailExistsAsync(employee.Email))
                {
                    ModelState.AddModelError("Email", "This email address is already registered.");
                }

                // Check for duplicate Aadhar
                //if (!string.IsNullOrEmpty(employee.Aadhar) && await _employeeRepository.AadharExistsAsync(employee.Aadhar))
                //{
                //    ModelState.AddModelError("Aadhar", "This Aadhar number is already registered.");
                //}

                // Check for duplicate PAN
                //if (!string.IsNullOrEmpty(employee.Pan) && await _employeeRepository.PanExistsAsync(employee.Pan))
                //{
                //    ModelState.AddModelError("Pan", "This PAN number is already registered.");
                //}

                if (ModelState.IsValid)
                {
                    // Generate employee code if not provided
                    if (string.IsNullOrEmpty(employee.Code))
                    {
                        employee.Code = await GenerateEmployeeCode();
                    }

                    // Set audit fields
                    employee.CreatedAt = DateTime.UtcNow;
                    employee.IsActive = true;

                    var employeeId = await _employeeRepository.CreateEmployeeAsync(employee);

                    // Return JSON response for AJAX requests
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            message = $"Employee '{employee.Name}' created successfully!",
                            employeeId = employeeId
                        });
                    }

                    TempData["SuccessMessage"] = $"Employee '{employee.Name}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                // Return validation errors for AJAX requests
                if (IsAjaxRequest())
                {
                    var errors = GetModelStateErrors();
                    return Json(new { success = false, errors = errors, message = "Please correct the validation errors." });
                }

                // Traditional form submission fallback
                await LoadViewBagData();
                return View(employee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Create: {ex.Message}");
                var errorMessage = "An error occurred while creating the employee. Please try again.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
                await LoadViewBagData();
                return View(employee);
            }
        }

        // POST: Edit employee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee)
        {
            try
            {
                // Get existing employee to preserve certain fields
                var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(Convert.ToInt32(employee.Id));
                if (existingEmployee == null)
                {
                    var notFoundMessage = "Employee not found.";
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = notFoundMessage });
                    }
                    TempData["ErrorMessage"] = notFoundMessage;
                    return RedirectToAction(nameof(Index));
                }

                // Set default values for nullable fields
                employee.TravelAllowance ??= 0;
                employee.MedicalAllowance ??= 0;
                employee.OtherAllowance ??= 0;
                employee.PF ??= 0;
                employee.ProfessionalTax ??= 0;
                employee.ESI ??= 0;

                // Auto-calculate HRA if not provided or zero
                if (!employee.HouseRentAllowance.HasValue || employee.HouseRentAllowance <= 0)
                {
                    if (employee.BasicSalary.HasValue && employee.BasicSalary > 0)
                    {
                        var maxHRA = (employee.BasicSalary.Value * 0.5m) * 2;
                        employee.HouseRentAllowance = Math.Min(maxHRA, employee.BasicSalary.Value);
                    }
                    else
                    {
                        employee.HouseRentAllowance = 0;
                    }
                }

                // Calculate gross salary after deductions
                var totalDeductions = employee.PF.Value + employee.ProfessionalTax.Value + employee.ESI.Value;
                employee.GrossSalaryAfterDeductions = (employee.TotalSalary ?? 0) - totalDeductions;

                // Preserve certain fields that shouldn't be updated via form
                employee.Code = existingEmployee.Code;
                employee.CreatedAt = existingEmployee.CreatedAt;
                employee.IsActive = existingEmployee.IsActive;

                // Validate business rules
                await ValidateEmployeeBusinessRules(employee);

                // Check for duplicate email (exclude current employee)
                if (await _employeeRepository.EmailExistsAsync(employee.Email, employee.Id))
                {
                    ModelState.AddModelError("Email", "This email address is already registered with another employee.");
                }

                //// Check for duplicate Aadhar (exclude current employee)
                //if (!string.IsNullOrEmpty(employee.Aadhar) && await _employeeRepository.AadharExistsAsync(employee.Aadhar, employee.Id))
                //{
                //    ModelState.AddModelError("Aadhar", "This Aadhar number is already registered with another employee.");
                //}

                //// Check for duplicate PAN (exclude current employee)
                //if (!string.IsNullOrEmpty(employee.Pan) && await _employeeRepository.PanExistsAsync(employee.Pan, employee.Id))
                //{
                //    ModelState.AddModelError("Pan", "This PAN number is already registered with another employee.");
                //}

                if (ModelState.IsValid)
                {
                    employee.UpdatedAt = DateTime.UtcNow;
                    var success = await _employeeRepository.UpdateEmployeeAsync(employee);

                    if (success)
                    {
                        if (IsAjaxRequest())
                        {
                            return Json(new { success = true, message = $"Employee '{employee.Name}' updated successfully!" });
                        }

                        TempData["SuccessMessage"] = $"Employee '{employee.Name}' updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        var updateFailMessage = "Failed to update employee. Please try again.";
                        if (IsAjaxRequest())
                        {
                            return Json(new { success = false, message = updateFailMessage });
                        }
                        TempData["ErrorMessage"] = updateFailMessage;
                    }
                }

                // Return validation errors for AJAX requests
                if (IsAjaxRequest())
                {
                    var errors = GetModelStateErrors();
                    return Json(new { success = false, errors = errors, message = "Please correct the validation errors." });
                }

                // Traditional form submission fallback
                await LoadViewBagData();
                return View(employee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Edit: {ex.Message}");
                var errorMessage = "An error occurred while updating the employee. Please try again.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
                await LoadViewBagData();
                return View(employee);
            }
        }

        // POST: Delete employee
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _employeeRepository.DeleteEmployeeAsync(id);

                if (success)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Employee deleted successfully!" });
                    }
                    TempData["SuccessMessage"] = "Employee deleted successfully!";
                }
                else
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Failed to delete employee." });
                    }
                    TempData["ErrorMessage"] = "Failed to delete employee.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Delete: {ex.Message}");
                var errorMessage = "An error occurred while deleting the employee.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
            }

            return IsAjaxRequest() ? Json(new { success = false }) : RedirectToAction(nameof(Index));
        }

        // GET: Validate Aadhar uniqueness for AJAX validation
        [HttpGet]
        public async Task<IActionResult> ValidateAadhar(string aadhar, int employeeId = 0)
        {
            try
            {
                //if (string.IsNullOrEmpty(aadhar) || aadhar.Length != 12)
                //{
                    return Json(new { isUnique = true });
               // }

                //var exists = await _employeeRepository.AadharExistsAsync(aadhar, employeeId);
                //return Json(new { isUnique = !exists });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating Aadhar: {ex.Message}");
                return Json(new { isUnique = true }); // Default to allow if validation fails
            }
        }

        // GET: Validate PAN uniqueness for AJAX validation
        [HttpGet]
        public async Task<IActionResult> ValidatePan(string pan, int employeeId = 0)
        {
            try
            {
                //if (string.IsNullOrEmpty(pan) || pan.Length != 10)
                //{
                    return Json(new { isUnique = true });
                //}

                //var exists = await _employeeRepository.PanExistsAsync(pan, employeeId);
                //return Json(new { isUnique = !exists });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating PAN: {ex.Message}");
                return Json(new { isUnique = true }); // Default to allow if validation fails
            }
        }

        // GET: Validate Email uniqueness for AJAX validation
        [HttpGet]
        public async Task<IActionResult> ValidateEmail(string email, int employeeId = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { isUnique = true });
                }

                var exists = await _employeeRepository.EmailExistsAsync(email, employeeId);
                return Json(new { isUnique = !exists });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating Email: {ex.Message}");
                return Json(new { isUnique = true }); // Default to allow if validation fails
            }
        }

        #region Private Helper Methods

        private async Task ValidateEmployeeBusinessRules(Employee employee)
        {
            // Validate age (minimum 18 years)
            if (employee.DateOfBirth.HasValue)
            {
                var age = DateTime.Now.Year - employee.DateOfBirth.Value.Year;
                if (DateTime.Now.DayOfYear < employee.DateOfBirth.Value.DayOfYear)
                    age--;

                if (age < 18)
                {
                    ModelState.AddModelError("DateOfBirth", "Employee must be at least 18 years old.");
                }
            }

            // Validate joining date (not in future)
            if (employee.DateOfJoining.HasValue && employee.DateOfJoining.Value.Date > DateTime.Now.Date)
            {
                ModelState.AddModelError("DateOfJoining", "Joining date cannot be in the future.");
            }

            // Validate joining date vs birth date
            if (employee.DateOfJoining.HasValue && employee.DateOfBirth.HasValue)
            {
                if (employee.DateOfJoining.Value.Date < employee.DateOfBirth.Value.Date)
                {
                    ModelState.AddModelError("DateOfJoining", "Joining date cannot be before date of birth.");
                }
            }

            // Validate basic salary doesn't exceed total salary
            if (employee.BasicSalary.HasValue && employee.TotalSalary.HasValue)
            {
                if (employee.BasicSalary.Value > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("BasicSalary", "Basic salary cannot exceed total salary.");
                }
            }

            // Validate salary and deductions
            if (employee.TotalSalary.HasValue && employee.TotalSalary > 0)
            {
                var totalDeductions = (employee.PF ?? 0) + (employee.ProfessionalTax ?? 0) + (employee.ESI ?? 0);
                if (totalDeductions > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("", "Total deductions cannot exceed total salary.");
                }

                // Validate HRA against basic salary
                if (employee.BasicSalary.HasValue && employee.HouseRentAllowance.HasValue)
                {
                    var maxHRA = (employee.BasicSalary.Value * 0.5m) * 2; // (Basic × 50%) × 2
                    if (employee.HouseRentAllowance.Value > maxHRA)
                    {
                        ModelState.AddModelError("HouseRentAllowance",
                            $"HRA cannot exceed ₹{maxHRA:F2} (Basic Salary × 50% × 2).");
                    }
                }

                // Validate PF limits
                if (employee.BasicSalary.HasValue && employee.PF.HasValue)
                {
                    var maxPF = Math.Min(employee.BasicSalary.Value * 0.12m, 1800);
                    if (employee.PF.Value > maxPF)
                    {
                        ModelState.AddModelError("PF", $"PF cannot exceed ₹{maxPF:F2} (12% of basic salary, max ₹1,800).");
                    }
                }

                // Validate Professional Tax
                if (employee.ProfessionalTax.HasValue && employee.ProfessionalTax.Value > 0)
                {
                    decimal maxPT = 0;
                    if (employee.TotalSalary.Value > 25000)
                        maxPT = 200;
                    else if (employee.TotalSalary.Value > 21000)
                        maxPT = 150;

                    if (employee.ProfessionalTax.Value > maxPT)
                    {
                        if (maxPT == 0)
                        {
                            ModelState.AddModelError("ProfessionalTax", "Professional Tax not applicable for salary ≤ ₹21,000.");
                        }
                        else
                        {
                            ModelState.AddModelError("ProfessionalTax", $"Professional Tax cannot exceed ₹{maxPT} for this salary range.");
                        }
                    }
                }

                // Validate ESI limits
                if (employee.ESI.HasValue && employee.ESI.Value > 0)
                {
                    //if (employee.TotalSalary.Value > 25000)
                    //{
                    //    ModelState.AddModelError("ESI", "ESI is not applicable for salary above ₹25,000.");
                    //}
                    //else
                    //{
                    //    var maxESI = employee.TotalSalary.Value * 0.0075m;
                    //    if (employee.ESI.Value > maxESI)
                    //    {
                    //        ModelState.AddModelError("ESI", $"ESI cannot exceed ₹{maxESI:F2} (0.75% of salary).");
                    //    }
                    //}
                }

                // Validate gross salary is positive
                if (employee.GrossSalaryAfterDeductions < 0)
                {
                    ModelState.AddModelError("", "Gross salary after deductions cannot be negative. Please adjust the deduction amounts.");
                }
            }
        }

        private async Task<string> GenerateEmployeeCode()
        {
            try
            {
                var year = DateTime.Now.Year;
                var prefix = $"EMP{year}";

                var lastEmployeeCode = "";//await _employeeRepository.GetLastEmployeeCodeAsync(prefix);

                int nextNumber = 1;
                if (!string.IsNullOrEmpty(lastEmployeeCode) && lastEmployeeCode.Length > prefix.Length)
                {
                    var numberPart = lastEmployeeCode.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                return $"{prefix}{nextNumber:D4}"; // EMP2025001, EMP2025002, etc.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Code Generation Error: {ex.Message}");
                return $"EMP{DateTime.Now.Year}{new Random().Next(1000, 9999)}";
            }
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers.ContainsKey("X-Requested-With") &&
                   Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        private Dictionary<string, string[]> GetModelStateErrors()
        {
            return ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
        }

        private async Task LoadViewBagData()
        {
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
        }

        #endregion

        #region Backward Compatibility Methods (Optional)

        // Keep these if you need traditional MVC views alongside AJAX functionality

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        public async Task<IActionResult> Create()
        {
            await LoadViewBagData();
            return View();
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            await LoadViewBagData();
            return View(employee);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        #endregion
    }
}