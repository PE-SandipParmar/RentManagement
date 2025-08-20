//using Microsoft.AspNetCore.Mvc;
//using RentManagement.Data;
//using RentManagement.Models;

//namespace RentManagement.Controllers
//{
//    public class EmployeeController : Controller
//    {
//        private readonly IEmployeeRepository _employeeRepository;

//        public EmployeeController(IEmployeeRepository employeeRepository)
//        {
//            _employeeRepository = employeeRepository;
//        }

//        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
//        {
//            var employees = await _employeeRepository.GetEmployeesAsync(page, pageSize, search);
//            ViewBag.Search = search;
//            ViewBag.PageSize = pageSize;

//            // Load departments and designations for the slide-over drawers
//            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
//            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();

//            return View(employees);
//        }

//        [HttpPost]
//        public IActionResult ToggleActive(int id)
//        {
//            _employeeRepository.ToggleActiveStatus(id);
//            return RedirectToAction(nameof(Index));
//        }

//        // NEW: Get employee data for slide-over drawers (Edit and Details)
//        [HttpGet]
//        public async Task<IActionResult> GetEmployee(int id)
//        {
//            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
//            if (employee == null)
//            {
//                return Json(new { success = false, message = "Employee not found" });
//            }

//            return Json(new
//            {
//                success = true,
//                employee = new
//                {
//                    id = employee.Id,
//                    code = employee.Code,
//                    name = employee.Name,
//                    dateOfBirth = employee.DateOfBirth?.ToString("yyyy-MM-dd"),
//                    gender = employee.Gender,
//                    email = employee.Email,
//                    aadhar = employee.Aadhar,
//                    pan = employee.Pan,
//                    departmentId = employee.DepartmentId,
//                    departmentName = employee.DepartmentName,
//                    designationId = employee.DesignationId,
//                    designationName = employee.DesignationName,
//                    dateOfJoining = employee.DateOfJoining?.ToString("yyyy-MM-dd"),
//                    eligibleForLease = employee.EligibleForLease,
//                    totalSalary = employee.TotalSalary,
//                    houseRentAllowance = employee.HouseRentAllowance,
//                    travelAllowance = employee.TravelAllowance,
//                    medicalAllowance = employee.MedicalAllowance,
//                    otherAllowance = employee.OtherAllowance,
//                    grossSalaryAfterDeductions = employee.GrossSalaryAfterDeductions,
//                    isActive = employee.IsActive,
//                    createdAt = employee.CreatedAt
//                }
//            });
//        }

//        // Keep original Details method for backward compatibility (if needed)
//        public async Task<IActionResult> Details(int id)
//        {
//            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
//            if (employee == null)
//                return NotFound();

//            return View(employee);
//        }

//        // Keep original Create GET method for backward compatibility (if needed)
//        public async Task<IActionResult> Create()
//        {
//            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
//            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
//            return View();
//        }

//        // MODIFIED: Create POST method to support both AJAX and traditional form submission
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Employee employee)
//        {
//            // Validation logic
//            if (await _employeeRepository.EmailExistsAsync(employee.Email))
//            {
//                ModelState.AddModelError("Email", "This email address is already registered.");
//            }



//            if (ModelState.IsValid)
//            {
//                var employeeId = await _employeeRepository.CreateEmployeeAsync(employee);
//                TempData["SuccessMessage"] = "Employee created successfully!";

//                // Check if it's an AJAX request
//                if (Request.Headers.ContainsKey("X-Requested-With") &&
//                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//                {
//                    return Json(new { success = true, message = "Employee created successfully!" });
//                }

//                return RedirectToAction(nameof(Index));
//            }

//            // Handle validation errors for AJAX requests
//            if (Request.Headers.ContainsKey("X-Requested-With") &&
//                Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//            {
//                var errors = ModelState
//                    .Where(x => x.Value.Errors.Count > 0)
//                    .ToDictionary(
//                        kvp => kvp.Key,
//                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
//                    );

//                return Json(new { success = false, errors = errors });
//            }

//            // Traditional form submission fallback
//            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
//            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
//            return View(employee);
//        }

//        // Keep original Edit GET method for backward compatibility (if needed)
//        public async Task<IActionResult> Edit(int id)
//        {
//            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
//            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
//            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
//            if (employee == null)
//                return NotFound();

//            return View(employee);
//        }

//        // MODIFIED: Edit POST method to support both AJAX and traditional form submission
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(Employee employee)
//        {
//            // Validation logic
//            if (await _employeeRepository.EmailExistsAsync(employee.Email, employee.Id))
//            {
//                ModelState.AddModelError("Email", "This email address is already registered.");
//            }

//            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
//            {
//                decimal monthlySalary = employee.TotalSalary.Value / 12;
//                if (employee.HouseRentAllowance.Value > monthlySalary)
//                {
//                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month's salary.");
//                }
//            }

//            if (ModelState.IsValid)
//            {
//                var success = await _employeeRepository.UpdateEmployeeAsync(employee);
//                if (success)
//                {
//                    TempData["SuccessMessage"] = "Employee updated successfully!";

//                    // Check if it's an AJAX request
//                    if (Request.Headers.ContainsKey("X-Requested-With") &&
//                        Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//                    {
//                        return Json(new { success = true, message = "Employee updated successfully!" });
//                    }

//                    return RedirectToAction(nameof(Index));
//                }
//                else
//                {
//                    TempData["ErrorMessage"] = "Failed to update employee.";

//                    if (Request.Headers.ContainsKey("X-Requested-With") &&
//                        Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//                    {
//                        return Json(new { success = false, message = "Failed to update employee." });
//                    }
//                }
//            }

//            // Handle validation errors for AJAX requests
//            if (Request.Headers.ContainsKey("X-Requested-With") &&
//                Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//            {
//                var errors = ModelState
//                    .Where(x => x.Value.Errors.Count > 0)
//                    .ToDictionary(
//                        kvp => kvp.Key,
//                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
//                    );

//                return Json(new { success = false, errors = errors });
//            }

//            // Traditional form submission fallback
//            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
//            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
//            return View(employee);
//        }

//        // Keep original Delete GET method for backward compatibility (if needed)
//        public async Task<IActionResult> Delete(int id)
//        {
//            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
//            if (employee == null)
//                return NotFound();

//            return View(employee);
//        }

//        // MODIFIED: DeleteConfirmed method to support both AJAX and traditional form submission
//        [HttpPost, ActionName("DeleteConfirmed")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var success = await _employeeRepository.DeleteEmployeeAsync(id);

//            if (success)
//            {
//                TempData["SuccessMessage"] = "Employee deleted successfully!";
//            }
//            else
//            {
//                TempData["ErrorMessage"] = "Failed to delete employee.";
//            }

//            // Check if it's an AJAX request
//            if (Request.Headers.ContainsKey("X-Requested-With") &&
//                Request.Headers["X-Requested-With"] == "XMLHttpRequest")
//            {
//                if (success)
//                {
//                    return Json(new { success = true, message = "Employee deleted successfully!" });
//                }
//                else
//                {
//                    return Json(new { success = false, message = "Failed to delete employee." });
//                }
//            }

//            return RedirectToAction(nameof(Index));
//        }
//    }
//}

using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;

namespace RentManagement.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            try
            {
                // Debug logging to troubleshoot the HouseRentAllowance issue
                System.Diagnostics.Debug.WriteLine($"Received Employee Data:");
                System.Diagnostics.Debug.WriteLine($"Name: {employee.Name}");
                System.Diagnostics.Debug.WriteLine($"Total Salary: {employee.TotalSalary}");
                System.Diagnostics.Debug.WriteLine($"HRA: {employee.HouseRentAllowance}");
                System.Diagnostics.Debug.WriteLine($"PF: {employee.PF}");
                System.Diagnostics.Debug.WriteLine($"Professional Tax: {employee.ProfessionalTax}");
                System.Diagnostics.Debug.WriteLine($"ESI: {employee.ESI}");
                System.Diagnostics.Debug.WriteLine($"ModelState Valid: {ModelState.IsValid}");
                // Set default values for other allowances if null
                employee.TravelAllowance ??= 0;
                employee.MedicalAllowance ??= 0;
                employee.OtherAllowance ??= 0;
                employee.PF ??= 0;
                employee.ProfessionalTax ??= 0;
                employee.ESI ??= 0;
                // Log all ModelState errors for debugging
                if (!ModelState.IsValid)
                {
                    foreach (var kvp in ModelState)
                    {
                        if (kvp.Value.Errors.Count > 0)
                        {
                            var errors = string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage));
                            System.Diagnostics.Debug.WriteLine($"ModelState Error - {kvp.Key}: {errors}");
                        }
                    }
                }

                // Set default values for calculated fields if they're null or zero
                if (!employee.HouseRentAllowance.HasValue || employee.HouseRentAllowance <= 0)
                {
                    if (employee.TotalSalary.HasValue && employee.TotalSalary > 0)
                    {
                        // Auto-calculate HRA: (Total Salary × 20%) × 2, max = Total Salary
                        var hraCalculated = Math.Min((employee.TotalSalary.Value * 0.20m) * 2, employee.TotalSalary.Value);
                        employee.HouseRentAllowance = hraCalculated;
                        System.Diagnostics.Debug.WriteLine($"Auto-calculated HRA: {employee.HouseRentAllowance}");
                    }
                    else
                    {
                        employee.HouseRentAllowance = 0;
                    }
                }

                

                // Calculate gross salary after deductions
                var totalDeductions = employee.PF.Value + employee.ProfessionalTax.Value + employee.ESI.Value;
                employee.GrossSalaryAfterDeductions = (employee.TotalSalary ?? 0) - totalDeductions;

                // Additional business rule validations
                await ValidateEmployeeBusinessRules(employee);

                // Check existing email
                if (await _employeeRepository.EmailExistsAsync(employee.Email))
                {
                    ModelState.AddModelError("Email", "This email address is already registered.");
                }

                // Check for Aadhar uniqueness (assuming you have this method in repository)
                if (!string.IsNullOrEmpty(employee.Aadhar))
                {
                    //var aadharExists = await _employeeRepository.AadharExistsAsync(employee.Aadhar);
                    //if (aadharExists)
                    //{
                    //    ModelState.AddModelError("Aadhar", "This Aadhar number is already registered.");
                    //}
                }

                // Check for PAN uniqueness (assuming you have this method in repository)
                if (!string.IsNullOrEmpty(employee.Pan))
                {
                    //var panExists = await _employeeRepository.PanExistsAsync(employee.Pan);
                    //if (panExists)
                    //{
                    //    ModelState.AddModelError("Pan", "This PAN number is already registered.");
                    //}
                }

                if (ModelState.IsValid)
                {
                    // Generate employee code if not provided
                    if (string.IsNullOrEmpty(employee.Code))
                    {
                       // employee.Code = await GenerateEmployeeCode();
                    }

                    // Set audit fields
                    employee.CreatedAt = DateTime.UtcNow;
                    employee.IsActive = true;

                    var employeeId = await _employeeRepository.CreateEmployeeAsync(employee);

                    System.Diagnostics.Debug.WriteLine($"Employee created successfully with ID: {employeeId}");

                    TempData["SuccessMessage"] = $"Employee '{employee.Name}' created successfully!";

                    // Check if it's an AJAX request
                    if (Request.Headers.ContainsKey("X-Requested-With") &&
                        Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new
                        {
                            success = true,
                            message = $"Employee '{employee.Name}' created successfully!",
                            employeeId = employeeId
                        });
                    }

                    return RedirectToAction(nameof(Index));
                }

                // Handle validation errors for AJAX requests
                if (Request.Headers.ContainsKey("X-Requested-With") &&
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return Json(new { success = false, errors = errors, message = "Please correct the validation errors." });
                }

                // Traditional form submission fallback
                ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
                ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
                return View(employee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Create: {ex.Message}");

                var errorMessage = "An error occurred while creating the employee. Please try again.";

                if (Request.Headers.ContainsKey("X-Requested-With") &&
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
                ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
                ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
                return View(employee);
            }
        }

        // Helper method to validate business rules
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

            // Validate salary deductions
            if (employee.TotalSalary.HasValue && employee.TotalSalary > 0)
            {
                var totalDeductions = (employee.PF ?? 0) + (employee.ProfessionalTax ?? 0) + (employee.ESI ?? 0);
                if (totalDeductions > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("", "Total deductions cannot exceed total salary.");
                }

                // Validate PF limit (12% of basic salary, max ₹1,800)
                var basicSalary = employee.TotalSalary.Value * 0.5m; // Assuming basic is 50% of total
                var maxPF = Math.Min(basicSalary * 0.12m, 1800);
                if (employee.PF > maxPF)
                {
                    ModelState.AddModelError("PF", $"PF cannot exceed ₹{maxPF:F2} (12% of basic salary, max ₹1,800).");
                }

                // Validate Professional Tax limits
                decimal maxPT = 0;
                if (employee.TotalSalary.Value > 25000)
                    maxPT = 200;
                else if (employee.TotalSalary.Value > 21000)
                    maxPT = 150;

                if (employee.ProfessionalTax > maxPT)
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

                // Validate ESI limits (only for salary ≤ ₹25,000)
                if (employee.TotalSalary.Value > 25000 && employee.ESI > 0)
                {
                    ModelState.AddModelError("ESI", "ESI is not applicable for salary above ₹25,000.");
                }
                else if (employee.TotalSalary.Value <= 25000)
                {
                    var maxESI = employee.TotalSalary.Value * 0.0075m;
                    if (employee.ESI > maxESI)
                    {
                        ModelState.AddModelError("ESI", $"ESI cannot exceed ₹{maxESI:F2} (0.75% of salary).");
                    }
                }

                // Validate HRA (shouldn't exceed total salary)
                if (employee.HouseRentAllowance > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot exceed total salary.");
                }
            }
        }
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

        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            _employeeRepository.ToggleActiveStatus(id);
            return RedirectToAction(nameof(Index));
        }

        // NEW: Get employee data for slide-over drawers (Edit and Details)
        [HttpGet]
        public async Task<IActionResult> GetEmployee(int id)
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
                    houseRentAllowanceDisplay = employee.HouseRentAllowance,
                    houseRentAllowance = employee.HouseRentAllowance,
                    travelAllowance = employee.TravelAllowance,
                    medicalAllowance = employee.MedicalAllowance,
                    otherAllowance = employee.OtherAllowance,
                    grossSalaryAfterDeductions = employee.GrossSalaryAfterDeductions,
                    pf = employee.PF,
                    professionalTax = employee.ProfessionalTax,
                    esi = employee.ESI,
                    isActive = employee.IsActive,
                    createdAt = employee.CreatedAt
                }
            });
        }

        // Keep original Details method for backward compatibility (if needed)
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // Keep original Create GET method for backward compatibility (if needed)
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
            return View();
        }

        
        // Keep original Edit GET method for backward compatibility (if needed)
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // MODIFIED: Edit POST method to support both AJAX and traditional form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee)
        {
            try
            {
                // Debug logging to troubleshoot issues
                System.Diagnostics.Debug.WriteLine($"Updating Employee Data:");
                System.Diagnostics.Debug.WriteLine($"ID: {employee.Id}");
                System.Diagnostics.Debug.WriteLine($"Name: {employee.Name}");
                System.Diagnostics.Debug.WriteLine($"Total Salary: {employee.TotalSalary}");
                System.Diagnostics.Debug.WriteLine($"HRA: {employee.HouseRentAllowance}");
                System.Diagnostics.Debug.WriteLine($"PF: {employee.PF}");
                System.Diagnostics.Debug.WriteLine($"Professional Tax: {employee.ProfessionalTax}");
                System.Diagnostics.Debug.WriteLine($"ESI: {employee.ESI}");
                System.Diagnostics.Debug.WriteLine($"ModelState Valid: {ModelState.IsValid}");


                // Set default values for other allowances if null
                employee.TravelAllowance ??= 0;
                employee.MedicalAllowance ??= 0;
                employee.OtherAllowance ??= 0;
                employee.PF ??= 0;
                employee.ProfessionalTax ??= 0;
                employee.ESI ??= 0;

                // Log all ModelState errors for debugging
                if (!ModelState.IsValid)
                {
                    foreach (var kvp in ModelState)
                    {
                        if (kvp.Value.Errors.Count > 0)
                        {
                            var errors = string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage));
                            System.Diagnostics.Debug.WriteLine($"ModelState Error - {kvp.Key}: {errors}");
                        }
                    }
                }

                // Get existing employee to preserve certain fields and check if exists
                var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(Convert.ToInt32(employee.Id));
                if (existingEmployee == null)
                {
                    var notFoundMessage = "Employee not found.";
                    if (Request.Headers.ContainsKey("X-Requested-With") &&
                        Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = notFoundMessage });
                    }
                    TempData["ErrorMessage"] = notFoundMessage;
                    return RedirectToAction(nameof(Index));
                }

                // Set default values for calculated fields if they're null or zero
                if (!employee.HouseRentAllowance.HasValue || employee.HouseRentAllowance <= 0)
                {
                    if (employee.TotalSalary.HasValue && employee.TotalSalary > 0)
                    {
                        // Auto-calculate HRA: (Total Salary × 20%) × 2, max = Total Salary
                        var hraCalculated = Math.Min((employee.TotalSalary.Value * 0.20m) * 2, employee.TotalSalary.Value);
                        employee.HouseRentAllowance = hraCalculated;
                        System.Diagnostics.Debug.WriteLine($"Auto-calculated HRA: {employee.HouseRentAllowance}");
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
                employee.Code = existingEmployee.Code; // Employee code should not change
                employee.CreatedAt = existingEmployee.CreatedAt; // Preserve creation date
                employee.IsActive = existingEmployee.IsActive; // Status changed via separate action

                // Additional business rule validations
                await ValidateEmployeeBusinessRulesForUpdate(employee);

                // Check existing email (exclude current employee)
                if (await _employeeRepository.EmailExistsAsync(employee.Email, employee.Id))
                {
                    ModelState.AddModelError("Email", "This email address is already registered with another employee.");
                }

                // Check for Aadhar uniqueness (exclude current employee)
                if (!string.IsNullOrEmpty(employee.Aadhar))
                {
                    //var aadharExists = await _employeeRepository.AadharExistsAsync(employee.Aadhar, employee.Id);
                    //if (aadharExists)
                    //{
                    //    ModelState.AddModelError("Aadhar", "This Aadhar number is already registered with another employee.");
                    //}
                }

                // Check for PAN uniqueness (exclude current employee)
                if (!string.IsNullOrEmpty(employee.Pan))
                {
                    //var panExists = await _employeeRepository.PanExistsAsync(employee.Pan, employee.Id);
                    //if (panExists)
                    //{
                    //    ModelState.AddModelError("Pan", "This PAN number is already registered with another employee.");
                    //}
                }

                if (ModelState.IsValid)
                {
                    // Set audit fields for update
                    employee.UpdatedAt = DateTime.UtcNow;

                    var success = await _employeeRepository.UpdateEmployeeAsync(employee);

                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Employee updated successfully with ID: {employee.Id}");

                        TempData["SuccessMessage"] = $"Employee '{employee.Name}' updated successfully!";

                        // Check if it's an AJAX request
                        if (Request.Headers.ContainsKey("X-Requested-With") &&
                            Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new
                            {
                                success = true,
                                message = $"Employee '{employee.Name}' updated successfully!"
                            });
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        var updateFailMessage = "Failed to update employee. Please try again.";
                        TempData["ErrorMessage"] = updateFailMessage;

                        if (Request.Headers.ContainsKey("X-Requested-With") &&
                            Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, message = updateFailMessage });
                        }
                    }
                }

                // Handle validation errors for AJAX requests
                if (Request.Headers.ContainsKey("X-Requested-With") &&
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return Json(new { success = false, errors = errors, message = "Please correct the validation errors." });
                }

                // Traditional form submission fallback
                ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
                ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
                return View(employee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Edit: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                var errorMessage = "An error occurred while updating the employee. Please try again.";

                if (Request.Headers.ContainsKey("X-Requested-With") &&
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }

                TempData["ErrorMessage"] = errorMessage;
                ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
                ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
                return View(employee);
            }
        }

        private async Task ValidateEmployeeBusinessRulesForUpdate(Employee employee)
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

            // For updates, joining date validation should be more flexible
            // Allow past dates but not future dates
            if (employee.DateOfJoining.HasValue && employee.DateOfJoining.Value.Date > DateTime.Now.Date)
            {
                ModelState.AddModelError("DateOfJoining", "Joining date cannot be in the future.");
            }

            // Validate salary deductions
            if (employee.TotalSalary.HasValue && employee.TotalSalary > 0)
            {
                var totalDeductions = (employee.PF ?? 0) + (employee.ProfessionalTax ?? 0) + (employee.ESI ?? 0);
                if (totalDeductions > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("", "Total deductions cannot exceed total salary.");
                }

                // Validate PF limit (12% of basic salary, max ₹1,800)
                var basicSalary = employee.TotalSalary.Value * 0.5m; // Assuming basic is 50% of total
                var maxPF = Math.Min(basicSalary * 0.12m, 1800);
                if (employee.PF > maxPF)
                {
                    ModelState.AddModelError("PF", $"PF cannot exceed ₹{maxPF:F2} (12% of basic salary, max ₹1,800).");
                }

                // Validate Professional Tax limits
                decimal maxPT = 0;
                if (employee.TotalSalary.Value > 25000)
                    maxPT = 200;
                else if (employee.TotalSalary.Value > 21000)
                    maxPT = 150;

                if (employee.ProfessionalTax > maxPT)
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

                // Validate ESI limits (only for salary ≤ ₹25,000)
                if (employee.TotalSalary.Value > 25000 && employee.ESI > 0)
                {
                    ModelState.AddModelError("ESI", "ESI is not applicable for salary above ₹25,000.");
                }
                else if (employee.TotalSalary.Value <= 25000)
                {
                    var maxESI = employee.TotalSalary.Value * 0.0075m;
                    if (employee.ESI > maxESI)
                    {
                        ModelState.AddModelError("ESI", $"ESI cannot exceed ₹{maxESI:F2} (0.75% of salary).");
                    }
                }

                // Validate HRA (shouldn't exceed total salary)
                if (employee.HouseRentAllowance > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot exceed total salary.");
                }

                // Additional validation: Total allowances shouldn't exceed total salary
                var totalAllowances = (employee.HouseRentAllowance ?? 0) +
                                     (employee.TravelAllowance ?? 0) +
                                     (employee.MedicalAllowance ?? 0) +
                                     (employee.OtherAllowance ?? 0);

                if (totalAllowances > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("", "Total allowances cannot exceed total salary.");
                }

                // Validate gross salary is positive
                if (employee.GrossSalaryAfterDeductions < 0)
                {
                    ModelState.AddModelError("", "Gross salary after deductions cannot be negative. Please adjust the deduction amounts.");
                }
            }

            // Validate that joining date is not after today (for updates)
            if (employee.DateOfJoining.HasValue && employee.DateOfBirth.HasValue)
            {
                if (employee.DateOfJoining.Value.Date < employee.DateOfBirth.Value.Date)
                {
                    ModelState.AddModelError("DateOfJoining", "Joining date cannot be before date of birth.");
                }
            }
        }

        // Additional method to update only specific fields (if needed)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSalary(int id, decimal totalSalary, decimal? hra = null,
            decimal? pf = null, decimal? professionalTax = null, decimal? esi = null)
        {
            try
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found." });
                }

                // Update salary related fields
                employee.TotalSalary = totalSalary;
                employee.HouseRentAllowance = hra ?? Math.Min((totalSalary * 0.20m) * 2, totalSalary);
                employee.PF = pf ?? 0;
                employee.ProfessionalTax = professionalTax ?? 0;
                employee.ESI = esi ?? 0;

                // Recalculate gross salary
                var totalDeductions = employee.PF.Value + employee.ProfessionalTax.Value + employee.ESI.Value;
                employee.GrossSalaryAfterDeductions = totalSalary - totalDeductions;
                employee.UpdatedAt = DateTime.UtcNow;

                var success = await _employeeRepository.UpdateEmployeeAsync(employee);

                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Salary updated successfully!",
                        grossSalary = employee.GrossSalaryAfterDeductions
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update salary." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSalary: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating salary." });
            }
        }

        // Keep original Delete GET method for backward compatibility (if needed)
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // MODIFIED: DeleteConfirmed method to support both AJAX and traditional form submission
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _employeeRepository.DeleteEmployeeAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Employee deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete employee.";
            }

            // Check if it's an AJAX request
            if (Request.Headers.ContainsKey("X-Requested-With") &&
                Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (success)
                {
                    return Json(new { success = true, message = "Employee deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete employee." });
                }
            }

            return RedirectToAction(nameof(Index));
        }
        // Helper method to generate employee code
        //private async Task<string> GenerateEmployeeCode()
        //{
        //    try
        //    {
        //        var year = DateTime.Now.Year;
        //        var prefix = $"EMP{year}";

        //        // You'll need to add this method to your repository
        //        var lastEmployeeCode = await _employeeRepository.GetLastEmployeeCodeAsync(prefix);

        //        int nextNumber = 1;
        //        if (!string.IsNullOrEmpty(lastEmployeeCode) && lastEmployeeCode.Length > prefix.Length)
        //        {
        //            var numberPart = lastEmployeeCode.Substring(prefix.Length);
        //            if (int.TryParse(numberPart, out int lastNumber))
        //            {
        //                nextNumber = lastNumber + 1;
        //            }
        //        }

        //        return $"{prefix}{nextNumber:D4}"; // EMP2025001, EMP2025002, etc.
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Code Generation Error: {ex.Message}");
        //        return $"EMP{DateTime.Now.Year}{new Random().Next(1000, 9999)}";
        //    }
        //}

        // Add this method for Aadhar validation (referenced in your JavaScript)
        //[HttpGet]
        //public async Task<IActionResult> ValidateAadhar(string aadhar, int employeeId = 0)
        //{
        //    try
        //    {
        //        var exists = await _employeeRepository.AadharExistsAsync(aadhar, employeeId);
        //        return Json(new { isUnique = !exists });
        //    }
        //    catch
        //    {
        //        return Json(new { isUnique = true }); // Default to allow if validation fails
        //    }
        //}
    }
}