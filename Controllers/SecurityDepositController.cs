using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RentManagement.Controllers
{
    [Authorize]
    public class SecurityDepositController : Controller
    {
        private readonly ISecurityDepositRepository _securityDepositRepository;
        private readonly ILogger<SecurityDepositController> _logger;

        public SecurityDepositController(ISecurityDepositRepository securityDepositRepository,
            ILogger<SecurityDepositController> logger)
        {
            _securityDepositRepository = securityDepositRepository;
            _logger = logger;
        }

        // GET: SecurityDeposit
        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                await LoadDropdowns();

                var viewModel = new SecurityDepositListViewModel
                {
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    ApprovalStatusFilter = approvalStatusFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CurrentUserRole = userRole,
                    ShowApprovalSection = true // Show for all roles now
                };

                // Load different data based on user role and filter
                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    // Set default approval status filter to show All Status by default
                    if (string.IsNullOrEmpty(approvalStatusFilter))
                    {
                        approvalStatusFilter = "All Status";
                        viewModel.ApprovalStatusFilter = "All Status";
                    }

                    if (approvalStatusFilter == "All Status")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Approved")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetApprovedSecurityDepositsAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetApprovedSecurityDepositCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetRejectedSecurityDepositsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetRejectedSecurityDepositCountAsync(searchTerm);
                    }

                    // Load pending approvals for the approval section
                    viewModel.PendingApprovals = (await _securityDepositRepository.GetPendingApprovalsAsync("", 1, 10)).ToList();
                }
                else
                {
                    // Makers - Show All Status by default, but allow viewing all statuses
                    if (string.IsNullOrEmpty(approvalStatusFilter))
                    {
                        approvalStatusFilter = "All Status";
                        viewModel.ApprovalStatusFilter = "All Status";
                    }

                    if (approvalStatusFilter == "All Status")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Approved")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetApprovedSecurityDepositsAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetApprovedSecurityDepositCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        viewModel.SecurityDeposits = (await _securityDepositRepository.GetRejectedSecurityDepositsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _securityDepositRepository.GetRejectedSecurityDepositCountAsync(searchTerm);
                    }

                    // Load pending approvals for makers (their own submissions)
                    viewModel.PendingApprovals = (await _securityDepositRepository.GetPendingApprovalsAsync("", 1, 10)).ToList();
                }

                ViewBag.PageSize = pageSize;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching security deposits");
                TempData["ErrorMessage"] = "An error occurred while loading security deposits.";
                return View(new SecurityDepositListViewModel { CurrentUserRole = GetCurrentUserRole() });
            }
        }

        // AJAX: Get security deposit details for view/edit
        [HttpGet]
        public async Task<IActionResult> GetSecurityDepositDetails(int id)
        {
            try
            {
                var deposit = await _securityDepositRepository.GetByIdAsync(id);
                if (deposit == null)
                {
                    return Json(new { success = false, message = "Security deposit not found." });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Id = deposit.Id,
                        EmployeeId = deposit.EmployeeId,
                        EmployeeName = deposit.EmployeeName,
                        VendorId = deposit.VendorId,
                        VendorName = deposit.VendorName,
                        LeaseId = deposit.LeaseId,
                        LeaseName = deposit.LeaseName,
                        Amount = deposit.Amount,
                        TdsRate = deposit.TdsRate,
                        TdsAmount = deposit.TdsAmount,
                        Remark = deposit.Remark,
                        Status = deposit.IsActive ? "Active" : "Inactive",
                        ApprovalStatus = (int)deposit.ApprovalStatus,
                        ApprovalStatusText = deposit.ApprovalStatusText,
                        MakerUserName = deposit.MakerUserName,
                        CheckerUserName = deposit.CheckerUserName,
                        MakerAction = (int)deposit.MakerAction,
                        MakerActionText = deposit.MakerAction.ToString(),
                        ApprovalDate = deposit.ApprovalDate,
                        RejectionReason = deposit.RejectionReason,
                        CreatedDate = deposit.CreatedDate,
                        ModifiedDate = deposit.ModifiedDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching security deposit details for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while fetching security deposit details." });
            }
        }

        // AJAX: Create security deposit (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> CreateSecurityDeposit([FromBody] SecurityDepositCreateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var deposit = new SecurityDeposit
                {
                    EmployeeId = request.EmployeeId,
                    VendorId = request.VendorId,
                    LeaseId = request.LeaseId,
                    Amount = request.Amount,
                    TdsRate = request.TdsRate,
                    TdsAmount = request.TdsAmount,
                    Remark = request.Remark,
                    IsActive = request.Status == "Active"
                };

                // Validate model
                if (!TryValidateModel(deposit))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                // Check for duplicate
                if (await _securityDepositRepository.IsDuplicateRecordAsync(deposit.EmployeeId, deposit.LeaseId, deposit.VendorId))
                {
                    return Json(new { success = false, message = "A security deposit record already exists for this Employee + Lease + Owner combination." });
                }

                int depositId;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly create approved deposits
                    deposit.ApprovalStatus = ApprovalStatus.Approved;
                    deposit.MakerUserId = userId;
                    deposit.MakerUserName = userName;
                    deposit.CheckerUserId = userId;
                    deposit.CheckerUserName = userName;
                    deposit.MakerAction = MakerAction.Create;
                    deposit.ApprovalDate = DateTime.Now;
                    deposit.CreatedBy = userName;
                    depositId = await _securityDepositRepository.CreateAsync(deposit);
                    message = "Security deposit created successfully.";
                }
                else
                {
                    // Maker role - create deposit for approval
                    depositId = await _securityDepositRepository.AddSecurityDepositForApprovalAsync(deposit, userId, userName, MakerAction.Create);
                    message = "Security deposit created successfully and sent for approval.";
                }

                if (depositId > 0)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create security deposit." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating security deposit");
                return Json(new { success = false, message = "An error occurred while creating the security deposit." });
            }
        }

        // AJAX: Update security deposit (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> UpdateSecurityDeposit([FromBody] SecurityDepositUpdateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var deposit = await _securityDepositRepository.GetByIdAsync(request.Id);
                if (deposit == null)
                {
                    return Json(new { success = false, message = "Security deposit not found." });
                }

                // Check if deposit has pending changes
                if (await _securityDepositRepository.HasPendingChangesAsync(request.Id))
                {
                    return Json(new { success = false, message = "This security deposit has pending approval changes. Please wait for approval before making new changes." });
                }

                // Update deposit properties
                deposit.EmployeeId = request.EmployeeId;
                deposit.VendorId = request.VendorId;
                deposit.LeaseId = request.LeaseId;
                deposit.Amount = request.Amount;
                deposit.TdsRate = request.TdsRate;
                deposit.TdsAmount = request.TdsAmount;
                deposit.Remark = request.Remark;
                deposit.IsActive = request.Status == "Active";

                // Validate model
                if (!TryValidateModel(deposit))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                // Check for duplicate (excluding current record)
                if (await _securityDepositRepository.IsDuplicateRecordAsync(deposit.EmployeeId, deposit.LeaseId, deposit.VendorId, deposit.Id))
                {
                    return Json(new { success = false, message = "A security deposit record already exists for this Employee + Lease + Owner combination." });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly update approved deposits
                    deposit.CheckerUserId = userId;
                    deposit.CheckerUserName = userName;
                    deposit.ApprovalDate = DateTime.Now;
                    deposit.ModifiedBy = userName;
                    success = await _securityDepositRepository.UpdateAsync(deposit);
                    message = "Security deposit updated successfully.";
                }
                else
                {
                    // Maker role - update deposit for approval
                    success = await _securityDepositRepository.UpdateSecurityDepositForApprovalAsync(deposit, userId, userName);
                    message = "Security deposit updated successfully and sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update security deposit." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating security deposit with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while updating the security deposit." });
            }
        }

        // AJAX: Delete security deposit (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> DeleteSecurityDeposit(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Check if deposit has pending changes
                if (await _securityDepositRepository.HasPendingChangesAsync(id))
                {
                    return Json(new { success = false, message = "This security deposit has pending approval changes. Please wait for approval before making new changes." });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly delete deposits
                    success = await _securityDepositRepository.DeleteAsync(id, 0);
                    message = "Security deposit deleted successfully.";
                }
                else
                {
                    // Maker role - mark deposit for deletion approval
                    success = await _securityDepositRepository.DeleteSecurityDepositForApprovalAsync(id, userId, userName);
                    message = "Security deposit deletion request sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete security deposit." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting security deposit with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while deleting the security deposit." });
            }
        }

        // AJAX: Approve security deposit (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> ApproveSecurityDeposit(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var success = await _securityDepositRepository.ApproveSecurityDepositAsync(id, userId, userName);
                if (success)
                {
                    return Json(new { success = true, message = "Security deposit approved successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to approve security deposit." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving security deposit with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while approving the security deposit." });
            }
        }

        // AJAX: Reject security deposit (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> RejectSecurityDeposit([FromBody] SecurityDepositRejectionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (string.IsNullOrEmpty(request.RejectionReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var success = await _securityDepositRepository.RejectSecurityDepositAsync(request.Id, userId, userName, request.RejectionReason);
                if (success)
                {
                    return Json(new { success = true, message = "Security deposit rejected successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reject security deposit." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rejecting security deposit with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while rejecting the security deposit." });
            }
        }

        // AJAX: Get security deposits with pagination
        [HttpGet]
        public async Task<IActionResult> GetSecurityDeposits(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                IEnumerable<SecurityDeposit> deposits;
                int totalCount;

                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    if (approvalStatusFilter == "All Status" || string.IsNullOrEmpty(approvalStatusFilter))
                    {
                        deposits = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Approved")
                    {
                        deposits = await _securityDepositRepository.GetApprovedSecurityDepositsAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _securityDepositRepository.GetApprovedSecurityDepositCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        deposits = await _securityDepositRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                        totalCount = await _securityDepositRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        deposits = await _securityDepositRepository.GetRejectedSecurityDepositsAsync(searchTerm, page, pageSize);
                        totalCount = await _securityDepositRepository.GetRejectedSecurityDepositCountAsync(searchTerm);
                    }
                    else
                    {
                        deposits = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusCountAsync(searchTerm, statusFilter);
                    }
                }
                else // Maker role
                {
                    if (approvalStatusFilter == "All Status" || string.IsNullOrEmpty(approvalStatusFilter))
                    {
                        deposits = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Approved")
                    {
                        deposits = await _securityDepositRepository.GetApprovedSecurityDepositsAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _securityDepositRepository.GetApprovedSecurityDepositCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        deposits = await _securityDepositRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                        totalCount = await _securityDepositRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        deposits = await _securityDepositRepository.GetRejectedSecurityDepositsAsync(searchTerm, page, pageSize);
                        totalCount = await _securityDepositRepository.GetRejectedSecurityDepositCountAsync(searchTerm);
                    }
                    else
                    {
                        deposits = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _securityDepositRepository.GetAllSecurityDepositsWithApprovalStatusCountAsync(searchTerm, statusFilter);
                    }
                }

                var result = new
                {
                    success = true,
                    data = deposits.Select(d => new
                    {
                        d.Id,
                        d.EmployeeId,
                        d.EmployeeName,
                        d.VendorId,
                        d.VendorName,
                        d.LeaseId,
                        d.LeaseName,
                        d.Amount,
                        d.TdsRate,
                        d.TdsAmount,
                        NetAmount = d.NetAmount,
                        d.Remark,
                        Status = d.IsActive ? "Active" : "Inactive",
                        ApprovalStatus = (int)d.ApprovalStatus,
                        ApprovalStatusText = d.ApprovalStatusText,
                        d.MakerUserName,
                        d.CheckerUserName,
                        MakerAction = (int)d.MakerAction,
                        MakerActionText = d.MakerAction.ToString(),
                        d.ApprovalDate,
                        d.RejectionReason,
                        d.CreatedDate,
                        d.ModifiedDate
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
                _logger.LogError(ex, "Error occurred while fetching security deposits");
                return Json(new { success = false, message = "An error occurred while loading security deposits." });
            }
        }

        // Other existing methods remain the same
        public async Task<IActionResult> Details(int id)
        {
            var deposit = await _securityDepositRepository.GetByIdAsync(id);
            if (deposit == null)
                return NotFound();

            return View(deposit);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SecurityDeposit deposit)
        {
            // Custom validation for duplicate records
            if (await _securityDepositRepository.IsDuplicateRecordAsync(deposit.EmployeeId, deposit.LeaseId, deposit.VendorId))
            {
                ModelState.AddModelError("", "A security deposit record already exists for this Employee + Lease + Owner combination.");
            }

            // Validate amount against employee salary
            //if (deposit.EmployeeId > 0 && deposit.Amount > 0)
            //{
            //    var employeeSalary = await _securityDepositRepository.GetEmployeeSalaryAsync(deposit.EmployeeId);
            //    var maxAllowedAmount = employeeSalary * 2;

            //    if (deposit.Amount > maxAllowedAmount)
            //    {
            //        ModelState.AddModelError("Amount", $"Security deposit cannot exceed ₹{maxAllowedAmount:N2} (HRA * 2 of ₹{employeeSalary:N2})");
            //    }
            //}

            // Validate positive amount
            if (deposit.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than 0");
            }

            // Validate TDS Rate
            if (deposit.TdsRate.HasValue)
            {
                if (deposit.TdsRate < 0 || deposit.TdsRate > 100)
                {
                    ModelState.AddModelError("TdsRate", "TDS Rate must be between 0 and 100%");
                }
            }

            // Auto-calculate TDS Amount if TDS Rate is provided
            if (deposit.Amount > 0 && deposit.TdsRate.HasValue && deposit.TdsRate > 0)
            {
                deposit.TdsAmount = (deposit.Amount * deposit.TdsRate.Value) / 100;
            }
            else
            {
                deposit.TdsAmount = 0;
            }

            // Validate TDS Amount doesn't exceed Deposit Amount
            if (deposit.TdsAmount.HasValue && deposit.TdsAmount > deposit.Amount)
            {
                ModelState.AddModelError("TdsAmount", "TDS Amount cannot exceed Deposit Amount");
            }

            if (ModelState.IsValid)
            {
                var newId = await _securityDepositRepository.CreateAsync(deposit);
                TempData["SuccessMessage"] = "Security deposit created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(deposit);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var deposit = await _securityDepositRepository.GetByIdAsync(id);
            if (deposit == null)
                return NotFound();

            await LoadDropdowns();
            return View(deposit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SecurityDeposit deposit)
        {
            if (id != deposit.Id)
                return NotFound();

            // Custom validation for duplicate records (excluding current record)
            if (await _securityDepositRepository.IsDuplicateRecordAsync(deposit.EmployeeId, deposit.LeaseId, deposit.VendorId, deposit.Id))
            {
                ModelState.AddModelError("", "A security deposit record already exists for this Employee + Lease + Owner combination.");
            }

            // Validate amount against employee salary
            //if (deposit.EmployeeId > 0 && deposit.Amount > 0)
            //{
            //    var employeeSalary = await _securityDepositRepository.GetEmployeeSalaryAsync(deposit.EmployeeId);
            //    var maxAllowedAmount = employeeSalary * 2;

            //    if (deposit.Amount > maxAllowedAmount)
            //    {
            //        ModelState.AddModelError("Amount", $"Security deposit cannot exceed ₹{maxAllowedAmount:N2} (2 × Monthly Salary of ₹{employeeSalary:N2})");
            //    }
            //}

            // Validate positive amount
            if (deposit.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than 0");
            }

            // Validate TDS Rate
            if (deposit.TdsRate.HasValue)
            {
                if (deposit.TdsRate < 0 || deposit.TdsRate > 100)
                {
                    ModelState.AddModelError("TdsRate", "TDS Rate must be between 0 and 100%");
                }
            }

            // Auto-calculate TDS Amount if TDS Rate is provided
            if (deposit.Amount > 0 && deposit.TdsRate.HasValue && deposit.TdsRate > 0)
            {
                deposit.TdsAmount = (deposit.Amount * deposit.TdsRate.Value) / 100;
            }
            else
            {
                deposit.TdsAmount = 0;
            }

            // Validate TDS Amount doesn't exceed Deposit Amount
            if (deposit.TdsAmount.HasValue && deposit.TdsAmount > deposit.Amount)
            {
                ModelState.AddModelError("TdsAmount", "TDS Amount cannot exceed Deposit Amount");
            }

            if (ModelState.IsValid)
            {
                var success = await _securityDepositRepository.UpdateAsync(deposit);
                if (success)
                {
                    TempData["SuccessMessage"] = "Security deposit updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update security deposit.";
                }
            }

            await LoadDropdowns();
            return View(deposit);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var deposit = await _securityDepositRepository.GetByIdAsync(id);
            if (deposit == null)
                return NotFound();

            return View(deposit);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _securityDepositRepository.DeleteAsync(id, 0);
            if (success)
            {
                TempData["SuccessMessage"] = "Security deposit deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete security deposit.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _securityDepositRepository.ToggleActiveStatus(id);
            return RedirectToAction(nameof(Index));
        }

        // Existing AJAX methods remain the same
        [HttpGet]
        public async Task<JsonResult> GetEmployeeSalary(int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return Json(new { success = false, message = "Invalid employee ID" });
                }

                var salary = await _securityDepositRepository.GetEmployeeSalaryAsync(employeeId);

                if (salary > 0)
                {
                    return Json(new
                    {
                        success = true,
                        salary = salary,
                        maxSecurityDeposit = salary * 2
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Employee salary not found" });
                }
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving employee salary: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployeeLeases(int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return Json(new List<object>());
                }

                var leases = await _securityDepositRepository.GetLeasesByEmployeeAsync(employeeId);

                var result = leases.Select(l => new
                {
                    id = l.Id,
                    name = l.Name ?? $"Lease #{l.Id}"
                }).ToList();

                return Json(result);
            }
            catch (System.Exception)
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetLeaseOwner(int leaseId)
        {
            try
            {
                if (leaseId <= 0)
                {
                    return Json(new { success = false, message = "Invalid lease ID" });
                }

                var ownerId = await _securityDepositRepository.GetLeaseOwnerAsync(leaseId);

                if (ownerId > 0)
                {
                    return Json(new
                    {
                        success = true,
                        ownerId = ownerId
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Owner not found for this lease" });
                }
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving lease owner: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckDuplicateRecord(int employeeId, int leaseId, int vendorId, int? excludeId = null)
        {
            try
            {
                if (employeeId <= 0 || leaseId <= 0 || vendorId <= 0)
                {
                    return Json(new { isDuplicate = false });
                }

                bool isDuplicate;

                if (excludeId.HasValue)
                {
                    isDuplicate = await _securityDepositRepository.IsDuplicateRecordAsync(employeeId, leaseId, vendorId, excludeId.Value);
                }
                else
                {
                    isDuplicate = await _securityDepositRepository.IsDuplicateRecordAsync(employeeId, leaseId, vendorId);
                }

                return Json(new { isDuplicate = isDuplicate });
            }
            catch (System.Exception)
            {
                return Json(new { isDuplicate = false });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidateAmount(int employeeId, decimal amount)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return Json(new
                    {
                        isValid = false,
                        message = "Please select an employee first"
                    });
                }

                if (amount <= 0)
                {
                    return Json(new
                    {
                        isValid = false,
                        message = "Amount must be greater than 0"
                    });
                }

                var salary = await _securityDepositRepository.GetEmployeeSalaryAsync(employeeId);
                var maxAllowed = salary * 2;

                if (amount > maxAllowed)
                {
                    return Json(new
                    {
                        isValid = false,
                        message = $"Amount cannot exceed ₹{maxAllowed:N2} (2 × Monthly Salary)",
                        maxAllowed = maxAllowed,
                        salary = salary
                    });
                }

                return Json(new
                {
                    isValid = true,
                    message = "Amount is valid",
                    maxAllowed = maxAllowed,
                    salary = salary
                });
            }
            catch (System.Exception ex)
            {
                return Json(new
                {
                    isValid = false,
                    message = "Error validating amount: " + ex.Message
                });
            }
        }

        [HttpGet]
        public JsonResult CalculateTds(decimal amount, decimal tdsRate)
        {
            try
            {
                if (amount <= 0 || tdsRate <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        tdsAmount = 0,
                        netAmount = amount
                    });
                }

                if (tdsRate > 100)
                {
                    return Json(new
                    {
                        success = false,
                        message = "TDS Rate cannot exceed 100%",
                        tdsAmount = 0,
                        netAmount = amount
                    });
                }

                var tdsAmount = (amount * tdsRate) / 100;
                var netAmount = amount - tdsAmount;

                return Json(new
                {
                    success = true,
                    tdsAmount = Math.Round(tdsAmount, 2),
                    netAmount = Math.Round(netAmount, 2),
                    message = $"TDS: ₹{tdsAmount:N2}, Net Amount: ₹{netAmount:N2}"
                });
            }
            catch (System.Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error calculating TDS: " + ex.Message,
                    tdsAmount = 0,
                    netAmount = amount
                });
            }
        }

        // Helper methods
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

        private async Task LoadDropdowns()
        {
            ViewBag.Employees = await _securityDepositRepository.GetEmployeeNamesAsync();
            ViewBag.Vendors = await _securityDepositRepository.GetOwnersAsync();
            ViewBag.Leases = await _securityDepositRepository.GetLeaseNamesAsync();
        }
    }
}