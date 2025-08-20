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
                    houseRentAllowance = employee.HouseRentAllowance,
                    travelAllowance = employee.TravelAllowance,
                    medicalAllowance = employee.MedicalAllowance,
                    otherAllowance = employee.OtherAllowance,
                    grossSalaryAfterDeductions = employee.GrossSalaryAfterDeductions,
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

        // MODIFIED: Create POST method to support both AJAX and traditional form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            // Validation logic
            if (await _employeeRepository.EmailExistsAsync(employee.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }

            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
            {
                decimal monthlySalary = employee.TotalSalary.Value / 12;
                if (employee.HouseRentAllowance.Value > monthlySalary)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month's salary.");
                }
            }

            if (ModelState.IsValid)
            {
                var employeeId = await _employeeRepository.CreateEmployeeAsync(employee);
                TempData["SuccessMessage"] = "Employee created successfully!";

                // Check if it's an AJAX request
                if (Request.Headers.ContainsKey("X-Requested-With") &&
                    Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Employee created successfully!" });
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

                return Json(new { success = false, errors = errors });
            }

            // Traditional form submission fallback
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
            return View(employee);
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
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id)
                return NotFound();

            // Validation logic
            if (await _employeeRepository.EmailExistsAsync(employee.Email, employee.Id))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }

            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
            {
                decimal monthlySalary = employee.TotalSalary.Value / 12;
                if (employee.HouseRentAllowance.Value > monthlySalary)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month's salary.");
                }
            }

            if (ModelState.IsValid)
            {
                var success = await _employeeRepository.UpdateEmployeeAsync(employee);
                if (success)
                {
                    TempData["SuccessMessage"] = "Employee updated successfully!";

                    // Check if it's an AJAX request
                    if (Request.Headers.ContainsKey("X-Requested-With") &&
                        Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Employee updated successfully!" });
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update employee.";

                    if (Request.Headers.ContainsKey("X-Requested-With") &&
                        Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Failed to update employee." });
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

                return Json(new { success = false, errors = errors });
            }

            // Traditional form submission fallback
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
            return View(employee);
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

        // ALTERNATIVE: Separate AJAX-specific endpoints (if you prefer cleaner separation)

        // Alternative Create endpoint for AJAX only
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromForm] Employee employee)
        {
            // Validation logic
            if (await _employeeRepository.EmailExistsAsync(employee.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }

            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
            {
                decimal monthlySalary = employee.TotalSalary.Value / 12;
                if (employee.HouseRentAllowance.Value > monthlySalary)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month's salary.");
                }
            }

            if (ModelState.IsValid)
            {
                var employeeId = await _employeeRepository.CreateEmployeeAsync(employee);
                return Json(new { success = true, message = "Employee created successfully!" });
            }

            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { success = false, errors = errors });
        }

        // Alternative Edit endpoint for AJAX only
        [HttpPost]
        public async Task<IActionResult> EditAjax([FromForm] Employee employee)
        {
            // Validation logic
            if (await _employeeRepository.EmailExistsAsync(employee.Email, employee.Id))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }

            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
            {
                decimal monthlySalary = employee.TotalSalary.Value / 12;
                if (employee.HouseRentAllowance.Value > monthlySalary)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month's salary.");
                }
            }

            if (ModelState.IsValid)
            {
                var success = await _employeeRepository.UpdateEmployeeAsync(employee);
                if (success)
                {
                    return Json(new { success = true, message = "Employee updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update employee." });
                }
            }

            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { success = false, errors = errors });
        }

        // Alternative Delete endpoint for AJAX only
        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var success = await _employeeRepository.DeleteEmployeeAsync(id);

            if (success)
            {
                return Json(new { success = true, message = "Employee deleted successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to delete employee." });
            }
        }
    }
}