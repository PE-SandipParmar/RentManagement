using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;

namespace RentManagement.Controllers
{
    [Authorize]
    public class MonthlyRentPaymentController : Controller
    {
        private readonly ILeaseRepository _leaseRepository;
        private readonly IMonthlyRentPaymentRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly ILogger<MonthlyRentPaymentController> _logger;

        public MonthlyRentPaymentController(
            ILeaseRepository leaseRepository,
            IMonthlyRentPaymentRepository repository,
            IEmployeeRepository employeeRepository,
            IVendorRepository vendorRepository,
            ILogger<MonthlyRentPaymentController> logger)
        {
            _leaseRepository = leaseRepository;
            _repository = repository;
            _employeeRepository = employeeRepository;
            _vendorRepository = vendorRepository;
            _logger = logger;
        }

        // GET: MonthlyRentPayment - Fixed with proper filtering
        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "All", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();

                var viewModel = new MonthlyPaymentListViewModel
                {
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    ApprovalStatusFilter = approvalStatusFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CurrentUserRole = userRole,
                    ShowApprovalSection = userRole == UserRole.Checker || userRole == UserRole.Admin
                };

                // Load data based on approval status filter
                if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "All")
                {
                    // Get all payments with filters
                    viewModel.Payments = (await _repository.GetAllPaymentsAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _repository.GetAllPaymentsCountAsync(searchTerm, statusFilter);
                }
                else if (approvalStatusFilter == "Approved")
                {
                    viewModel.Payments = (await _repository.GetApprovedPaymentsAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _repository.GetApprovedPaymentCountAsync(searchTerm, statusFilter);
                }
                else if (approvalStatusFilter == "Pending")
                {
                    viewModel.Payments = (await _repository.GetPendingApprovalsAsync(searchTerm, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _repository.GetPendingApprovalCountAsync(searchTerm);
                }
                else if (approvalStatusFilter == "Rejected")
                {
                    viewModel.Payments = (await _repository.GetRejectedPaymentsAsync(searchTerm, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _repository.GetRejectedPaymentCountAsync(searchTerm);
                }

                // Load pending approvals for approval section (for checkers)
                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    viewModel.PendingApprovals = (await _repository.GetPendingApprovalsAsync("", 1, 5)).ToList();
                }

                // Load dropdowns
                ViewBag.Employees = await _employeeRepository.GetAllEmployeesDropdownAsync();
                ViewBag.TDSApplicable = await _repository.GetTdsApplicableAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching monthly payments");
                TempData["ErrorMessage"] = "An error occurred while loading payments.";
                return View(new MonthlyPaymentListViewModel { CurrentUserRole = GetCurrentUserRole() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorsByEmployee(int employeeId)
        {
            if (employeeId <= 0)
                return Json(new { success = false, message = "Invalid employee ID" });

            var vendors = await _repository.GetOwnersByEmployeeAsync(employeeId);
            return Json(new { success = true, vendors = vendors });
        }

        [HttpGet]
        public async Task<IActionResult> GetLeasesByEmployeeAndVendor(int employeeId, int vendorId)
        {
            if (employeeId <= 0 || vendorId <= 0)
                return Json(new { success = false, message = "Invalid employee or vendor ID" });

            var leases = await _repository.GetLeasesByEmployeeAndVendorAsync(employeeId, vendorId);
            return Json(new { success = true, leases = leases });
        }

        [HttpGet]
        public async Task<IActionResult> GetLeasesDetails(int id)
        {
            var lease = await _leaseRepository.GetLeaseByIdAsync(id);
            return Json(new { success = true, lease = lease });
        }

        // GET: MonthlyRentPayment/Create
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = await _repository.GetEmployeeNamesAsync();
            ViewBag.TDSApplicable = await _repository.GetTdsApplicableAsync();
            return View(new MonthlyRentPayment());
        }

        // POST: MonthlyRentPayment/Create - Fixed with proper validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Create(MonthlyRentPayment payment)
        {
            try
            {
                // Additional validation
                if (payment.EmployeeId <= 0)
                {
                    ModelState.AddModelError("EmployeeId", "Please select an employee.");
                }
                if (payment.VendorId <= 0)
                {
                    ModelState.AddModelError("VendorId", "Please select a vendor.");
                }
                if (payment.LeaseId <= 0)
                {
                    ModelState.AddModelError("LeaseId", "Please select a lease.");
                }

                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (ModelState.IsValid)
                {
                    int paymentId;
                    string message;

                    // Set default values
                    payment.PaymentStatus = string.IsNullOrEmpty(payment.PaymentStatus) ? "Pending" : payment.PaymentStatus;
                    payment.DSCApprovalStatus = string.IsNullOrEmpty(payment.DSCApprovalStatus) ? "Pending" : payment.DSCApprovalStatus;

                    if (userRole == UserRole.Admin)
                    {
                        // Admin can directly create approved payments
                        payment.ApprovalStatus = ApprovalStatus.Approved;
                        payment.MakerUserId = userId;
                        payment.MakerUserName = userName;
                        payment.CheckerUserId = userId;
                        payment.CheckerUserName = userName;
                        payment.MakerAction = MakerAction.Create;
                        payment.CheckerApprovalDate = DateTime.Now;
                        payment.CreatedBy = int.Parse(userId);
                        payment.IsActiveRecord = true;

                        paymentId = await _repository.CreateAsync(payment);
                        message = "Payment created successfully.";
                    }
                    else
                    {
                        // Maker role - create payment for approval
                        payment.CreatedBy = int.Parse(userId);
                        paymentId = await _repository.AddPaymentForApprovalAsync(payment, userId, userName, MakerAction.Create);
                        message = "Payment created successfully and sent for approval.";
                    }

                    if (paymentId > 0)
                    {
                        TempData["SuccessMessage"] = message;
                        return RedirectToAction(nameof(Index));
                    }
                }

                // If we got here, something failed
                ViewBag.Employees = await _repository.GetEmployeeNamesAsync();
                ViewBag.TDSApplicable = await _repository.GetTdsApplicableAsync();
                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating payment");
                TempData["ErrorMessage"] = "An error occurred while creating the payment.";
                ViewBag.Employees = await _repository.GetEmployeeNamesAsync();
                ViewBag.TDSApplicable = await _repository.GetTdsApplicableAsync();
                return View(payment);
            }
        }

        // GET: MonthlyRentPayment/Edit/5
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _repository.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            // Check if payment has pending changes
            if (await _repository.HasPendingChangesAsync(id))
            {
                TempData["ErrorMessage"] = "This payment has pending approval changes. Please wait for approval before making new changes.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Employees = await _repository.GetEmployeeNamesAsync();
            ViewBag.TDSApplicable = await _repository.GetTdsApplicableAsync();

            // Load vendors for the selected employee
            if (payment.EmployeeId > 0)
            {
                ViewBag.Vendors = await _repository.GetOwnersByEmployeeAsync(payment.EmployeeId);
            }

            // Load leases for the selected employee and vendor
            if (payment.EmployeeId > 0 && payment.VendorId > 0)
            {
                ViewBag.Leases = await _repository.GetLeasesByEmployeeAndVendorAsync(payment.EmployeeId, payment.VendorId);
            }

            return View(payment);
        }

        // POST: MonthlyRentPayment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Edit(int id, MonthlyRentPayment payment)
        {
            if (id != payment.Id)
            {
                return BadRequest();
            }

            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (ModelState.IsValid)
                {
                    bool success;
                    string message;

                    if (userRole == UserRole.Admin)
                    {
                        // Admin can directly update approved payments
                        payment.CheckerUserId = userId;
                        payment.CheckerUserName = userName;
                        payment.CheckerApprovalDate = DateTime.Now;
                        payment.ModifiedBy = int.Parse(userId);
                        success = await _repository.UpdateAsync(payment);
                        message = "Payment updated successfully.";
                    }
                    else
                    {
                        // Maker role - update payment for approval
                        payment.ModifiedBy = int.Parse(userId);
                        success = await _repository.UpdatePaymentForApprovalAsync(payment, userId, userName);
                        message = "Payment updated successfully and sent for approval.";
                    }

                    if (success)
                    {
                        TempData["SuccessMessage"] = message;
                        return RedirectToAction(nameof(Index));
                    }
                }

                ViewBag.Employees = await _repository.GetEmployeeNamesAsync();
                ViewBag.TDSApplicable = await _repository.GetTdsApplicableAsync();
                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating payment with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the payment.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: MonthlyRentPayment/Delete/5
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _repository.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            // Check if payment has pending changes
            if (await _repository.HasPendingChangesAsync(id))
            {
                TempData["ErrorMessage"] = "This payment has pending approval changes. Please wait for approval before making new changes.";
                return RedirectToAction(nameof(Index));
            }

            return View(payment);
        }

        // POST: MonthlyRentPayment/DeleteConfirmed
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

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly delete payments
                    success = await _repository.DeleteAsync(id);
                    message = "Payment deleted successfully.";
                }
                else
                {
                    // Maker role - mark payment for deletion approval
                    success = await _repository.DeletePaymentForApprovalAsync(id, userId, userName);
                    message = "Payment deletion request sent for approval.";
                }

                if (success)
                {
                    TempData["SuccessMessage"] = message;
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete payment.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting payment with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the payment.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MonthlyRentPayment/Approve
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> ApprovePayment(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var success = await _repository.ApprovePaymentAsync(id, userId, userName);
                if (success)
                {
                    return Json(new { success = true, message = "Payment approved successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to approve payment." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving payment with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while approving the payment." });
            }
        }

        // POST: MonthlyRentPayment/Reject
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> RejectPayment([FromBody] PaymentRejectionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (string.IsNullOrEmpty(request.RejectionReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var success = await _repository.RejectPaymentAsync(request.Id, userId, userName, request.RejectionReason);
                if (success)
                {
                    return Json(new { success = true, message = "Payment rejected successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reject payment." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rejecting payment with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while rejecting the payment." });
            }
        }

        // GET: MonthlyRentPayment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _repository.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
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
    }

    // Request models
    public class PaymentRejectionRequest
    {
        public int Id { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}