using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RentManagement.Data;
using RentManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RentManagement.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeRepository employeeRepository, ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        // GET: Employee Index with approval workflow
        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();

                var viewModel = new EmployeeListViewModel
                {
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    ApprovalStatusFilter = approvalStatusFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CurrentUserRole = userRole,
                    ShowApprovalSection = userRole == UserRole.Checker || userRole == UserRole.Admin
                };

                // Load different data based on user role and filter
                if (userRole == UserRole.Checker || userRole == UserRole.Admin || userRole == UserRole.Maker)
                {
                    // Checkers and Admins see approved employees by default

                    if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "Pending")
                    {
                        viewModel.Employees = (await _employeeRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _employeeRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Approved")
                    {
                        viewModel.Employees = (await _employeeRepository.GetApprovedEmployeesAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _employeeRepository.GetApprovedEmployeeCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        viewModel.Employees = (await _employeeRepository.GetRejectedEmployeesAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _employeeRepository.GetRejectedEmployeeCountAsync(searchTerm);
                    }

                    // Also load pending approvals for the approval section
                    viewModel.PendingApprovals = (await _employeeRepository.GetPendingApprovalsAsync("", 1, 5)).ToList();
                }
                else
                {
                    // Makers see only approved employees (they can't approve their own changes)
                    viewModel.Employees = (await _employeeRepository.GetApprovedEmployeesAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _employeeRepository.GetApprovedEmployeeCountAsync(searchTerm, statusFilter);
                }

                // Load departments and designations for the slide-over drawers
                ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
                ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employees");
                TempData["ErrorMessage"] = "An error occurred while loading employees.";
                return View(new EmployeeListViewModel());
            }
        }

        // GET: Get employees with pagination (for refresh after operations)
        [HttpGet]
        public async Task<IActionResult> GetEmployees(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                IEnumerable<Employee> employees;
                int totalCount;

                if (userRole == UserRole.Checker || userRole == UserRole.Admin || userRole == UserRole.Maker)
                {
                    if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "Approved")
                    {
                        employees = await _employeeRepository.GetApprovedEmployeesAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _employeeRepository.GetApprovedEmployeeCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        employees = await _employeeRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                        totalCount = await _employeeRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        employees = await _employeeRepository.GetRejectedEmployeesAsync(searchTerm, page, pageSize);
                        totalCount = await _employeeRepository.GetRejectedEmployeeCountAsync(searchTerm);
                    }
                    else
                    {
                        employees = await _employeeRepository.GetApprovedEmployeesAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _employeeRepository.GetApprovedEmployeeCountAsync(searchTerm, statusFilter);
                    }
                }
                else
                {
                    employees = await _employeeRepository.GetApprovedEmployeesAsync(searchTerm, statusFilter, page, pageSize);
                    totalCount = await _employeeRepository.GetApprovedEmployeeCountAsync(searchTerm, statusFilter);
                }

                var result = new
                {
                    success = true,
                    data = employees.Select(e => new
                    {
                        e.Id,
                        e.Code,
                        e.Name,
                        e.Email,
                        e.DepartmentName,
                        e.DesignationName,
                        e.TotalSalary,
                        e.IsActive,
                        ApprovalStatus = (int)e.ApprovalStatus,
                        ApprovalStatusText = e.ApprovalStatusText,
                        e.MakerUserName,
                        e.CheckerUserName,
                        MakerAction = (int)e.MakerAction,
                        MakerActionText = e.MakerAction.ToString(),
                        e.ApprovalDate,
                        e.RejectionReason,
                        e.CreatedAt,
                        e.UpdatedAt
                    }).ToList(),
                    pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employees");
                return Json(new { success = false, message = "An error occurred while loading employees." });
            }
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
                        hra = employee.HRA,
                        houseRentAllowance = employee.HouseRentAllowance,
                        travelAllowance = employee.TravelAllowance,
                        medicalAllowance = employee.MedicalAllowance,
                        otherAllowance = employee.OtherAllowance,
                        pf = employee.PF,
                        professionalTax = employee.ProfessionalTax,
                        esi = employee.ESI,
                        grossSalaryAfterDeductions = employee.GrossSalaryAfterDeductions,
                        isActive = employee.IsActive,
                        createdAt = employee.CreatedAt,
                        // Approval workflow fields
                        approvalStatus = (int)employee.ApprovalStatus,
                        approvalStatusText = employee.ApprovalStatusText,
                        makerUserName = employee.MakerUserName,
                        checkerUserName = employee.CheckerUserName,
                        makerAction = (int)employee.MakerAction,
                        makerActionText = employee.MakerAction.ToString(),
                        approvalDate = employee.ApprovalDate,
                        rejectionReason = employee.RejectionReason
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee: {Message}", ex.Message);
                return Json(new { success = false, message = "An error occurred while retrieving employee data." });
            }
        }

        // POST: Create new employee with approval workflow
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Create(Employee employee)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Set default values for nullable fields
                employee.TravelAllowance ??= 0;
                employee.MedicalAllowance ??= 0;
                employee.OtherAllowance ??= 0;
                employee.PF ??= 0;
                employee.ProfessionalTax ??= 0;
                employee.ESI ??= 0;
                employee.HouseRentAllowance ??= 0;
                employee.HRA ??= 0;

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

                    int employeeId;
                    string message;

                    if (userRole == UserRole.Admin)
                    {
                        // Admin can directly create approved employees
                        employee.ApprovalStatus = ApprovalStatus.Approved;
                        employee.MakerUserId = userId;
                        employee.MakerUserName = userName;
                        employee.CheckerUserId = userId;
                        employee.CheckerUserName = userName;
                        employee.MakerAction = MakerAction.Create;
                        employee.ApprovalDate = DateTime.Now;
                        employeeId = await _employeeRepository.CreateEmployeeAsync(employee);
                        message = "Employee created successfully.";
                    }
                    else
                    {
                        // Maker role - create employee for approval
                        employeeId = await _employeeRepository.AddEmployeeForApprovalAsync(employee, userId, userName, MakerAction.Create);
                        message = "Employee created successfully and sent for approval.";
                    }

                    if (employeeId > 0)
                    {
                        if (IsAjaxRequest())
                        {
                            return Json(new { success = true, message = message, employeeId = employeeId });
                        }

                        TempData["SuccessMessage"] = message;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        var failMessage = "Failed to create employee.";
                        if (IsAjaxRequest())
                        {
                            return Json(new { success = false, message = failMessage });
                        }
                        TempData["ErrorMessage"] = failMessage;
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
                _logger.LogError(ex, "Error in Create: {Message}", ex.Message);
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

        // POST: Edit employee with approval workflow
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Edit(Employee employee)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

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

                // Check if employee has pending changes
                if (await _employeeRepository.HasPendingChangesAsync(employee.Id.Value))
                {
                    var pendingMessage = "This employee has pending approval changes. Please wait for approval before making new changes.";
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = pendingMessage });
                    }
                    TempData["ErrorMessage"] = pendingMessage;
                    return RedirectToAction(nameof(Index));
                }

                // Set default values for nullable fields
                employee.TravelAllowance ??= 0;
                employee.MedicalAllowance ??= 0;
                employee.OtherAllowance ??= 0;
                employee.PF ??= 0;
                employee.ProfessionalTax ??= 0;
                employee.ESI ??= 0;
                employee.HRA ??= 0;

                // Auto-calculate HRA if not provided or zero
                if (!employee.HouseRentAllowance.HasValue || employee.HouseRentAllowance <= 0)
                {
                    if (employee.HRA.HasValue && employee.HRA > 0)
                    {
                        var maxHouseRentAllowance = (employee.HRA.Value  * 2);
                        employee.HouseRentAllowance = Math.Min(maxHouseRentAllowance, employee.HRA.Value);
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

                if (ModelState.IsValid)
                {
                    employee.UpdatedAt = DateTime.UtcNow;

                    bool success;
                    string message;

                    if (userRole == UserRole.Admin)
                    {
                        // Admin can directly update approved employees
                        employee.CheckerUserId = userId;
                        employee.CheckerUserName = userName;
                        employee.ApprovalDate = DateTime.Now;
                        success = await _employeeRepository.UpdateEmployeeAsync(employee);
                        message = "Employee updated successfully.";
                    }
                    else
                    {
                        // Maker role - update employee for approval
                        success = await _employeeRepository.UpdateEmployeeForApprovalAsync(employee, userId, userName);
                        message = "Employee updated successfully and sent for approval.";
                    }

                    if (success)
                    {
                        if (IsAjaxRequest())
                        {
                            return Json(new { success = true, message = message });
                        }

                        TempData["SuccessMessage"] = message;
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
                _logger.LogError(ex, "Error in Edit: {Message}", ex.Message);
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

        // POST: Delete employee with approval workflow
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Check if employee has pending changes
                if (await _employeeRepository.HasPendingChangesAsync(id))
                {
                    var pendingMessage = "This employee has pending approval changes. Please wait for approval before making new changes.";
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = pendingMessage });
                    }
                    TempData["ErrorMessage"] = pendingMessage;
                    return RedirectToAction(nameof(Index));
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly delete employees
                    success = await _employeeRepository.DeleteEmployeeAsync(id);
                    message = "Employee deleted successfully.";
                }
                else
                {
                    // Maker role - mark employee for deletion approval
                    success = await _employeeRepository.DeleteEmployeeForApprovalAsync(id, userId, userName);
                    message = "Employee deletion request sent for approval.";
                }

                if (success)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = true, message = message });
                    }
                    TempData["SuccessMessage"] = message;
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
                _logger.LogError(ex, "Error in Delete: {Message}", ex.Message);
                var errorMessage = "An error occurred while deleting the employee.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["ErrorMessage"] = errorMessage;
            }

            return IsAjaxRequest() ? Json(new { success = false }) : RedirectToAction(nameof(Index));
        }

        // AJAX: Approve employee (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> ApproveEmployee(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var success = await _employeeRepository.ApproveEmployeeAsync(id, userId, userName);
                if (success)
                {
                    return Json(new { success = true, message = "Employee approved successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to approve employee." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving employee with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while approving the employee." });
            }
        }

        // AJAX: Reject employee (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> RejectEmployee([FromBody] EmployeeRejectionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (string.IsNullOrEmpty(request.RejectionReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var success = await _employeeRepository.RejectEmployeeAsync(request.Id, userId, userName, request.RejectionReason);
                if (success)
                {
                    return Json(new { success = true, message = "Employee rejected successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reject employee." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rejecting employee with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while rejecting the employee." });
            }
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
                _logger.LogError(ex, "Error toggling status: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to update employee status.";
            }
            return RedirectToAction(nameof(Index));
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
                _logger.LogError(ex, "Error validating Email: {Message}", ex.Message);
                return Json(new { isUnique = true }); // Default to allow if validation fails
            }
        }

        // GET: Validate Aadhar uniqueness for AJAX validation
        [HttpGet]
        public async Task<IActionResult> ValidateAadhar(string aadhar, int employeeId = 0)
        {
            try
            {
                return Json(new { isUnique = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Aadhar: {Message}", ex.Message);
                return Json(new { isUnique = true }); // Default to allow if validation fails
            }
        }

        // GET: Validate PAN uniqueness for AJAX validation
        [HttpGet]
        public async Task<IActionResult> ValidatePan(string pan, int employeeId = 0)
        {
            try
            {
                return Json(new { isUnique = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating PAN: {Message}", ex.Message);
                return Json(new { isUnique = true }); // Default to allow if validation fails
            }
        }

        #region Helper Methods

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            return roleClaim switch
            {
                Roles.Admin => UserRole.Admin,
                Roles.Checker => UserRole.Checker,
                Roles.Maker => UserRole.Maker,
                _ => UserRole.Maker
            };
        }

        private string GetCurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
        }

        private string GetCurrentUserName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Unknown User";
        }

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
            // Validate HRA against total salary
            if (employee.HRA.HasValue && employee.BasicSalary.HasValue && employee.TotalSalary.HasValue)
            {
                decimal combinedAmount = employee.HRA.Value + employee.BasicSalary.Value;
                if (combinedAmount > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("HRA", "HRA and Basic Salary combined cannot exceed total salary.");
                }
            }
            else if (employee.HRA.HasValue && employee.TotalSalary.HasValue)
            {
                if (employee.HRA.Value > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("HRA", "HRA cannot exceed total salary.");
                }
            }
            //if (employee.HRA.HasValue && employee.HouseRentAllowance.HasValue)
            //{
            //    var maxHouseRentAllowance = (employee.HRA.Value * 0.5m) * 2; // (HRA × 50%) × 2
            //    if (employee.HouseRentAllowance.Value > maxHouseRentAllowance)
            //    {
            //        ModelState.AddModelError("HouseRentAllowance",
            //            $"House Rent Allowance cannot exceed ₹{maxHouseRentAllowance:F2} (HRA × 50% × 2).");
            //    }
            //}
            // Validate salary and deductions
            if (employee.TotalSalary.HasValue && employee.TotalSalary > 0)
            {
                var totalDeductions = (employee.PF ?? 0) + (employee.ProfessionalTax ?? 0) + (employee.ESI ?? 0);
                if (totalDeductions > employee.TotalSalary.Value)
                {
                    ModelState.AddModelError("", "Total deductions cannot exceed total salary.");
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

                // Updated: Validate House Rent Allowance against HRA (instead of basic salary)
                if (employee.HRA.HasValue && employee.HouseRentAllowance.HasValue)
                {
                    var maxHouseRentAllowance = (employee.HRA.Value * 2); // (HRA × 50%) × 2
                    if (employee.HouseRentAllowance.Value > maxHouseRentAllowance)
                    {
                        ModelState.AddModelError("HouseRentAllowance",
                            $"House Rent Allowance cannot exceed ₹{maxHouseRentAllowance:F2} (HRA × 50% × 2).");
                    }
                }

                // Validate PF limits
                if (employee.BasicSalary.HasValue && employee.PF.HasValue)
                {
                    var maxPF = Math.Min(employee.BasicSalary.Value * 0.12m, 1800);
                    if (employee.PF.Value > maxPF)
                    {
                        //ModelState.AddModelError("PF", $"PF cannot exceed ₹{maxPF:F2} (12% of basic salary, max ₹1,800).");
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
                            //ModelState.AddModelError("ProfessionalTax", "Professional Tax not applicable for salary ≤ ₹21,000.");
                        }
                        else
                        {
                            //ModelState.AddModelError("ProfessionalTax", $"Professional Tax cannot exceed ₹{maxPT} for this salary range.");
                        }
                    }
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
                _logger.LogError(ex, "Code Generation Error: {Message}", ex.Message);
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