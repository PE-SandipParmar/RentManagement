using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;

namespace RentManagement.Controllers
{
    [Authorize]
    public class LeaseController : Controller
    {
        private readonly ILeaseRepository _leaseRepository;
        private readonly ILogger<LeaseController> _logger;

        public LeaseController(ILeaseRepository leaseRepository, ILogger<LeaseController> logger)
        {
            _leaseRepository = leaseRepository;
            _logger = logger;
        }

        // GET: Lease
        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();

                var viewModel = new LeaseListViewModel
                {
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    ApprovalStatusFilter = approvalStatusFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CurrentUserRole = userRole,
                    ShowApprovalSection = userRole == UserRole.Checker || userRole == UserRole.Admin
                };

                // Load dropdown data
                await LoadViewBagData();

                // Load different data based on user role and filter
                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    // Checkers and Admins see approved leases by default
                    if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "Approved")
                    {
                        viewModel.Leases = (await _leaseRepository.GetApprovedLeasesAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _leaseRepository.GetApprovedLeaseCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        viewModel.Leases = (await _leaseRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _leaseRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        viewModel.Leases = (await _leaseRepository.GetRejectedLeasesAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _leaseRepository.GetRejectedLeaseCountAsync(searchTerm);
                    }

                    // Also load pending approvals for the approval section
                    viewModel.PendingApprovals = (await _leaseRepository.GetPendingApprovalsAsync("", 1, 5)).ToList();
                }
                else
                {
                    // Makers see only approved leases (they can't approve their own changes)
                    viewModel.Leases = (await _leaseRepository.GetApprovedLeasesAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _leaseRepository.GetApprovedLeaseCountAsync(searchTerm, statusFilter);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching leases");
                TempData["ErrorMessage"] = "An error occurred while loading leases.";
                return View(new LeaseListViewModel());
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetEmployeeHRA(int employeeId)
        {
            try
            {
                var employeeHRA = await _leaseRepository.GetEmployeeHRAAsync(employeeId);
                if (employeeHRA.HasValue)
                {
                    return Json(new
                    {
                        success = true,
                        hra = employeeHRA.Value,
                        maxAllowedRent = employeeHRA.Value * 2
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Employee HRA not found." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee HRA for ID: {EmployeeId}", employeeId);
                return Json(new { success = false, message = "An error occurred while fetching employee HRA." });
            }
        }
        // AJAX: Get lease details for view/edit
        [HttpGet]
        public async Task<IActionResult> GetLeaseDetails(int id)
        {
            try
            {
                var lease = await _leaseRepository.GetLeaseByIdAsync(id);
                if (lease == null)
                {
                    return Json(new { success = false, message = "Lease not found." });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Id = lease.Id,
                        PerquisiteType = lease.PerquisiteType,
                        Status = lease.Status,
                        LeaseTypeId = lease.LeaseTypeId,
                        RefNo = lease.RefNo,
                        EmployeeId = lease.EmployeeId,
                        RefDate = lease.RefDate?.ToString("yyyy-MM-dd"),
                        PerquisiteApplicablePercentId = lease.PerquisiteApplicablePercentId,
                        VendorId = lease.VendorId,
                        MonthlyRentPayable = lease.MonthlyRentPayable,
                        FromDate = lease.FromDate?.ToString("yyyy-MM-dd"),
                        EndDate = lease.EndDate?.ToString("yyyy-MM-dd"),
                        RentRecoveryElementId = lease.RentRecoveryElementId,
                        RentDeposit = lease.RentDeposit,
                        AdditionalRentRecovery = lease.AdditionalRentRecovery,
                        BrokerageAmount = lease.BrokerageAmount,
                        LicenseFeeRecoveryElementId = lease.LicenseFeeRecoveryElementId,
                        StampDuty = lease.StampDuty,
                        LicenseFeeAmount = lease.LicenseFeeAmount,
                        PaymentTermId = lease.PaymentTermId,
                        PayableOnOrBeforeId = lease.PayableOnOrBeforeId,
                        Narration = lease.Narration,
                        ApprovalStatus = (int)lease.ApprovalStatus,
                        ApprovalStatusText = lease.ApprovalStatusText,
                        MakerUserName = lease.MakerUserName,
                        CheckerUserName = lease.CheckerUserName,
                        MakerAction = (int)lease.MakerAction,
                        MakerActionText = lease.MakerActionText,
                        ApprovalDate = lease.ApprovalDate,
                        RejectionReason = lease.RejectionReason,
                        CreatedAt = lease.CreatedAt,
                        ModifiedAt = lease.ModifiedAt,
                        // Navigation properties
                        LeaseTypeName = lease.LeaseTypeName,
                        EmployeeName = lease.EmployeeName,
                        VendorName = lease.VendorName,
                        PerquisiteApplicablePercent = lease.PerquisiteApplicablePercent,
                        RentRecoveryElementName = lease.RentRecoveryElementName,
                        LicenseFeeRecoveryElementName = lease.LicenseFeeRecoveryElementName,
                        PaymentTermName = lease.PaymentTermName,
                        PayableOnOrBeforeName = lease.PayableOnOrBeforeName,
                        TotalLeaseAmount = lease.TotalLeaseAmount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching lease details for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while fetching lease details." });
            }
        }

        // AJAX: Create lease (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> CreateAjax1([FromBody] LeaseCreateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Check for duplicate lease reference number
                var existingLease = await _leaseRepository.GetLeaseByRefNoAsync(request.RefNo);
                if (existingLease != null)
                {
                    return Json(new { success = false, message = "Lease reference number already exists. Please use a different reference number." });
                }

                var lease = new Lease
                {
                    PerquisiteType = request.PerquisiteType,
                    Status = request.Status ?? "Active",
                    LeaseTypeId = request.LeaseTypeId,
                    RefNo = request.RefNo,
                    EmployeeId = request.EmployeeId,
                    RefDate = request.RefDate,
                    PerquisiteApplicablePercentId = request.PerquisiteApplicablePercentId,
                    VendorId = request.VendorId,
                    MonthlyRentPayable = request.MonthlyRentPayable,
                    FromDate = request.FromDate,
                    EndDate = request.EndDate,
                    RentRecoveryElementId = request.RentRecoveryElementId,
                    RentDeposit = request.RentDeposit,
                    AdditionalRentRecovery = request.AdditionalRentRecovery,
                    BrokerageAmount = request.BrokerageAmount,
                    LicenseFeeRecoveryElementId = request.LicenseFeeRecoveryElementId,
                    StampDuty = request.StampDuty,
                    LicenseFeeAmount = request.LicenseFeeAmount,
                    PaymentTermId = request.PaymentTermId,
                    PayableOnOrBeforeId = request.PayableOnOrBeforeId,
                    Narration = request.Narration,
                    IsActive = true,
                    CreatedBy = int.TryParse(userId, out int createdById) ? createdById : null
                };

                // Validate model
                if (!TryValidateModel(lease))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                int leaseId;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly create approved leases
                    lease.ApprovalStatus = ApprovalStatus.Approved;
                    lease.MakerUserId = userId;
                    lease.MakerUserName = userName;
                    lease.CheckerUserId = userId;
                    lease.CheckerUserName = userName;
                    lease.MakerAction = MakerAction.Create;
                    lease.ApprovalDate = DateTime.Now;
                    leaseId = await _leaseRepository.CreateLeaseAsync(lease);
                    message = "Lease created successfully.";
                }
                else
                {
                    // Maker role - create lease for approval
                    leaseId = await _leaseRepository.AddLeaseForApprovalAsync(lease, userId, userName, MakerAction.Create);
                    message = "Lease created successfully and sent for approval.";
                }

                if (leaseId > 0)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating lease");
                return Json(new { success = false, message = "An error occurred while creating the lease." });
            }
        }

        // AJAX: Update lease (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> UpdateAjax1([FromBody] LeaseUpdateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var lease = await _leaseRepository.GetLeaseByIdAsync(request.Id);
                if (lease == null)
                {
                    return Json(new { success = false, message = "Lease not found." });
                }

                // Check if lease has pending changes
                if (await _leaseRepository.HasPendingChangesAsync(request.Id))
                {
                    return Json(new { success = false, message = "This lease has pending approval changes. Please wait for approval before making new changes." });
                }

                // Check for duplicate lease reference number (excluding current lease)
                var existingLease = await _leaseRepository.GetLeaseByRefNoAsync(request.RefNo);
                if (existingLease != null && existingLease.Id != request.Id)
                {
                    return Json(new { success = false, message = "Lease reference number already exists. Please use a different reference number." });
                }

                // Update lease properties
                lease.PerquisiteType = request.PerquisiteType;
                lease.Status = request.Status ?? "Active";
                lease.LeaseTypeId = request.LeaseTypeId;
                lease.RefNo = request.RefNo;
                lease.EmployeeId = request.EmployeeId;
                lease.RefDate = request.RefDate;
                lease.PerquisiteApplicablePercentId = request.PerquisiteApplicablePercentId;
                lease.VendorId = request.VendorId;
                lease.MonthlyRentPayable = request.MonthlyRentPayable;
                lease.FromDate = request.FromDate;
                lease.EndDate = request.EndDate;
                lease.RentRecoveryElementId = request.RentRecoveryElementId;
                lease.RentDeposit = request.RentDeposit;
                lease.AdditionalRentRecovery = request.AdditionalRentRecovery;
                lease.BrokerageAmount = request.BrokerageAmount;
                lease.LicenseFeeRecoveryElementId = request.LicenseFeeRecoveryElementId;
                lease.StampDuty = request.StampDuty;
                lease.LicenseFeeAmount = request.LicenseFeeAmount;
                lease.PaymentTermId = request.PaymentTermId;
                lease.PayableOnOrBeforeId = request.PayableOnOrBeforeId;
                lease.Narration = request.Narration;
                lease.ModifiedBy = int.TryParse(userId, out int modifiedById) ? modifiedById : null;

                // Validate model
                if (!TryValidateModel(lease))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly update approved leases
                    lease.CheckerUserId = userId;
                    lease.CheckerUserName = userName;
                    lease.ApprovalDate = DateTime.Now;
                    success = await _leaseRepository.UpdateLeaseAsync(lease);
                    message = "Lease updated successfully.";
                }
                else
                {
                    // Maker role - update lease for approval
                    success = await _leaseRepository.UpdateLeaseForApprovalAsync(lease, userId, userName);
                    message = "Lease updated successfully and sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating lease with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while updating the lease." });
            }
        }

        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> CreateAjax([FromBody] LeaseCreateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Validate Monthly Rent against Employee HRA
                var employeeHRA = await _leaseRepository.GetEmployeeHRAAsync(request.EmployeeId);
                if (employeeHRA.HasValue && request.MonthlyRentPayable.HasValue)
                {
                    var maxAllowedRent = employeeHRA.Value * 2;
                    if (request.MonthlyRentPayable.Value > maxAllowedRent)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Monthly Rent Payable (₹{request.MonthlyRentPayable.Value:N2}) cannot exceed Employee HRA × 2 (₹{maxAllowedRent:N2})"
                        });
                    }
                }

                // Check for duplicate lease reference number
                var existingLease = await _leaseRepository.GetLeaseByRefNoAsync(request.RefNo);
                if (existingLease != null)
                {
                    return Json(new { success = false, message = "Lease reference number already exists. Please use a different reference number." });
                }

                // Rest of the existing CreateAjax method code remains the same...
                var lease = new Lease
                {
                    PerquisiteType = request.PerquisiteType,
                    Status = request.Status ?? "Active",
                    LeaseTypeId = request.LeaseTypeId,
                    RefNo = request.RefNo,
                    EmployeeId = request.EmployeeId,
                    RefDate = request.RefDate,
                    PerquisiteApplicablePercentId = request.PerquisiteApplicablePercentId,
                    VendorId = request.VendorId,
                    MonthlyRentPayable = request.MonthlyRentPayable,
                    FromDate = request.FromDate,
                    EndDate = request.EndDate,
                    RentRecoveryElementId = request.RentRecoveryElementId,
                    RentDeposit = request.RentDeposit,
                    AdditionalRentRecovery = request.AdditionalRentRecovery,
                    BrokerageAmount = request.BrokerageAmount,
                    LicenseFeeRecoveryElementId = request.LicenseFeeRecoveryElementId,
                    StampDuty = request.StampDuty,
                    LicenseFeeAmount = request.LicenseFeeAmount,
                    PaymentTermId = request.PaymentTermId,
                    PayableOnOrBeforeId = request.PayableOnOrBeforeId,
                    Narration = request.Narration,
                    IsActive = true,
                    CreatedBy = int.TryParse(userId, out int createdById) ? createdById : null
                };

                // Validate model
                if (!TryValidateModel(lease))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                int leaseId;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly create approved leases
                    lease.ApprovalStatus = ApprovalStatus.Approved;
                    lease.MakerUserId = userId;
                    lease.MakerUserName = userName;
                    lease.CheckerUserId = userId;
                    lease.CheckerUserName = userName;
                    lease.MakerAction = MakerAction.Create;
                    lease.ApprovalDate = DateTime.Now;
                    leaseId = await _leaseRepository.CreateLeaseAsync(lease);
                    message = "Lease created successfully.";
                }
                else
                {
                    // Maker role - create lease for approval
                    leaseId = await _leaseRepository.AddLeaseForApprovalAsync(lease, userId, userName, MakerAction.Create);
                    message = "Lease created successfully and sent for approval.";
                }

                if (leaseId > 0)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating lease");
                return Json(new { success = false, message = "An error occurred while creating the lease." });
            }
        }

        // Update the UpdateAjax method - Add HRA validation before updating lease
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> UpdateAjax([FromBody] LeaseUpdateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var lease = await _leaseRepository.GetLeaseByIdAsync(request.Id);
                if (lease == null)
                {
                    return Json(new { success = false, message = "Lease not found." });
                }

                // Check if lease has pending changes
                if (await _leaseRepository.HasPendingChangesAsync(request.Id))
                {
                    return Json(new { success = false, message = "This lease has pending approval changes. Please wait for approval before making new changes." });
                }

                // Validate Monthly Rent against Employee HRA
                var employeeHRA = await _leaseRepository.GetEmployeeHRAAsync(request.EmployeeId);
                if (employeeHRA.HasValue && request.MonthlyRentPayable.HasValue)
                {
                    var maxAllowedRent = employeeHRA.Value * 2;
                    if (request.MonthlyRentPayable.Value > maxAllowedRent)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Monthly Rent Payable (₹{request.MonthlyRentPayable.Value:N2}) cannot exceed Employee HRA × 2 (₹{maxAllowedRent:N2})"
                        });
                    }
                }

                // Check for duplicate lease reference number (excluding current lease)
                var existingLease = await _leaseRepository.GetLeaseByRefNoAsync(request.RefNo);
                if (existingLease != null && existingLease.Id != request.Id)
                {
                    return Json(new { success = false, message = "Lease reference number already exists. Please use a different reference number." });
                }

                // Rest of the existing UpdateAjax method code remains the same...
                // Update lease properties
                lease.PerquisiteType = request.PerquisiteType;
                lease.Status = request.Status ?? "Active";
                lease.LeaseTypeId = request.LeaseTypeId;
                lease.RefNo = request.RefNo;
                lease.EmployeeId = request.EmployeeId;
                lease.RefDate = request.RefDate;
                lease.PerquisiteApplicablePercentId = request.PerquisiteApplicablePercentId;
                lease.VendorId = request.VendorId;
                lease.MonthlyRentPayable = request.MonthlyRentPayable;
                lease.FromDate = request.FromDate;
                lease.EndDate = request.EndDate;
                lease.RentRecoveryElementId = request.RentRecoveryElementId;
                lease.RentDeposit = request.RentDeposit;
                lease.AdditionalRentRecovery = request.AdditionalRentRecovery;
                lease.BrokerageAmount = request.BrokerageAmount;
                lease.LicenseFeeRecoveryElementId = request.LicenseFeeRecoveryElementId;
                lease.StampDuty = request.StampDuty;
                lease.LicenseFeeAmount = request.LicenseFeeAmount;
                lease.PaymentTermId = request.PaymentTermId;
                lease.PayableOnOrBeforeId = request.PayableOnOrBeforeId;
                lease.Narration = request.Narration;
                lease.ModifiedBy = int.TryParse(userId, out int modifiedById) ? modifiedById : null;

                // Validate model
                if (!TryValidateModel(lease))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly update approved leases
                    lease.CheckerUserId = userId;
                    lease.CheckerUserName = userName;
                    lease.ApprovalDate = DateTime.Now;
                    success = await _leaseRepository.UpdateLeaseAsync(lease);
                    message = "Lease updated successfully.";
                }
                else
                {
                    // Maker role - update lease for approval
                    success = await _leaseRepository.UpdateLeaseForApprovalAsync(lease, userId, userName);
                    message = "Lease updated successfully and sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating lease with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while updating the lease." });
            }
        }
        // AJAX: Delete lease (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Check if lease has pending changes
                if (await _leaseRepository.HasPendingChangesAsync(id))
                {
                    return Json(new { success = false, message = "This lease has pending approval changes. Please wait for approval before making new changes." });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly delete leases
                    success = await _leaseRepository.DeleteLeaseAsync(id);
                    message = "Lease deleted successfully.";
                }
                else
                {
                    // Maker role - mark lease for deletion approval
                    success = await _leaseRepository.DeleteLeaseForApprovalAsync(id, userId, userName);
                    message = "Lease deletion request sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting lease with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while deleting the lease." });
            }
        }

        // AJAX: Approve lease (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> ApproveLease(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var success = await _leaseRepository.ApproveLeaseAsync(id, userId, userName);
                if (success)
                {
                    return Json(new { success = true, message = "Lease approved successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to approve lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving lease with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while approving the lease." });
            }
        }

        // AJAX: Reject lease (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> RejectLease([FromBody] LeaseRejectionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (string.IsNullOrEmpty(request.RejectionReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var success = await _leaseRepository.RejectLeaseAsync(request.Id, userId, userName, request.RejectionReason);
                if (success)
                {
                    return Json(new { success = true, message = "Lease rejected successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reject lease." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rejecting lease with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while rejecting the lease." });
            }
        }

        // AJAX: Get leases with pagination (for refresh after operations)
        [HttpGet]
        public async Task<IActionResult> GetLeases(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                IEnumerable<Lease> leases;
                int totalCount;

                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "Approved")
                    {
                        leases = await _leaseRepository.GetApprovedLeasesAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _leaseRepository.GetApprovedLeaseCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        leases = await _leaseRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                        totalCount = await _leaseRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        leases = await _leaseRepository.GetRejectedLeasesAsync(searchTerm, page, pageSize);
                        totalCount = await _leaseRepository.GetRejectedLeaseCountAsync(searchTerm);
                    }
                    else
                    {
                        leases = await _leaseRepository.GetApprovedLeasesAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _leaseRepository.GetApprovedLeaseCountAsync(searchTerm, statusFilter);
                    }
                }
                else
                {
                    leases = await _leaseRepository.GetApprovedLeasesAsync(searchTerm, statusFilter, page, pageSize);
                    totalCount = await _leaseRepository.GetApprovedLeaseCountAsync(searchTerm, statusFilter);
                }

                var result = new
                {
                    success = true,
                    data = leases.Select(l => new
                    {
                        l.Id,
                        l.RefNo,
                        l.LeaseTypeName,
                        l.EmployeeName,
                        l.VendorName,
                        l.MonthlyRentPayable,
                        FromDate = l.FromDate?.ToString("yyyy-MM-dd"),
                        EndDate = l.EndDate?.ToString("yyyy-MM-dd"),
                        l.Status,
                        ApprovalStatus = (int)l.ApprovalStatus,
                        ApprovalStatusText = l.ApprovalStatusText,
                        l.MakerUserName,
                        l.CheckerUserName,
                        MakerAction = (int)l.MakerAction,
                        MakerActionText = l.MakerActionText,
                        l.ApprovalDate,
                        l.RejectionReason,
                        l.CreatedAt,
                        l.ModifiedAt,
                        l.TotalLeaseAmount
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
                _logger.LogError(ex, "Error occurred while fetching leases");
                return Json(new { success = false, message = "An error occurred while loading leases." });
            }
        }

        // Helper methods
        private async Task LoadViewBagData()
        {
            ViewBag.LeaseTypes = await _leaseRepository.GetLeaseTypesAsync();
            ViewBag.EmployeeNames = await _leaseRepository.GetEmployeeNamesAsync();
            ViewBag.Owners = await _leaseRepository.GetOwnersAsync();
            ViewBag.RentRecoveryElements = await _leaseRepository.GetRentRecoveryElementsAsync();
            ViewBag.LicenseFeeRecoveryElements = await _leaseRepository.GetLicenseFeeRecoveryElementsAsync();
            ViewBag.PaymentTerms = await _leaseRepository.GetPaymentTermsAsync();
            ViewBag.PayableOnOrBeforeOptions = await _leaseRepository.GetPayableOnOrBeforeOptionsAsync();
            ViewBag.PerquisitePercents = await _leaseRepository.GetPerquisiteApplicablePercentsAsync();
        }

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

    // Request models for AJAX operations
    public class LeaseCreateRequest
    {
        public string PerquisiteType { get; set; } = "Non-Government";
        public string? Status { get; set; }
        public int LeaseTypeId { get; set; }
        public string RefNo { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public DateTime? RefDate { get; set; }
        public int PerquisiteApplicablePercentId { get; set; }
        public int VendorId { get; set; }
        public decimal? MonthlyRentPayable { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? RentRecoveryElementId { get; set; }
        public decimal? RentDeposit { get; set; }
        public decimal? AdditionalRentRecovery { get; set; }
        public decimal? BrokerageAmount { get; set; }
        public int? LicenseFeeRecoveryElementId { get; set; }
        public decimal? StampDuty { get; set; }
        public decimal? LicenseFeeAmount { get; set; }
        public int PaymentTermId { get; set; }
        public int PayableOnOrBeforeId { get; set; }
        public string Narration { get; set; } = string.Empty;
    }

    public class LeaseUpdateRequest : LeaseCreateRequest
    {
        public int Id { get; set; }
    }

    public class LeaseRejectionRequest
    {
        public int Id { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}