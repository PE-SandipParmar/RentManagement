using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;
using static RentManagement.Models.Vendor;

namespace RentManagement.Controllers
{
    [Authorize]
    public class VendorController : Controller
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<VendorController> _logger;

        public VendorController(IVendorRepository vendorRepository,
            IEmployeeRepository employeeRepository,
            ILogger<VendorController> logger)
        {
            _vendorRepository = vendorRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        // GET: Vendor
        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var employees = await _employeeRepository.GetAllEmployeesDropdownAsync();

                var viewModel = new VendorListViewModel
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
                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    // Checkers and Admins see approved vendors by default
                    if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "Approved")
                    {
                        viewModel.Vendors = (await _vendorRepository.GetApprovedVendorsAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _vendorRepository.GetApprovedVendorCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        viewModel.Vendors = (await _vendorRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _vendorRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        viewModel.Vendors = (await _vendorRepository.GetRejectedVendorsAsync(searchTerm, page, pageSize)).ToList();
                        viewModel.TotalRecords = await _vendorRepository.GetRejectedVendorCountAsync(searchTerm);
                    }

                    // Also load pending approvals for the approval section
                    viewModel.PendingApprovals = (await _vendorRepository.GetPendingApprovalsAsync("", 1, 5)).ToList();
                }
                else
                {
                    // Makers see only approved vendors (they can't approve their own changes)
                    viewModel.Vendors = (await _vendorRepository.GetApprovedVendorsAsync(searchTerm, statusFilter, page, pageSize)).ToList();
                    viewModel.TotalRecords = await _vendorRepository.GetApprovedVendorCountAsync(searchTerm, statusFilter);
                }

                ViewBag.Employees = employees.ToList();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching vendors");
                TempData["ErrorMessage"] = "An error occurred while loading vendors.";
                return View(new VendorListViewModel());
            }
        }

        // AJAX: Get vendor details for view/edit
        [HttpGet]
        public async Task<IActionResult> GetVendorDetails(int id)
        {
            try
            {
                var vendor = await _vendorRepository.GetVendorByIdAsync(id);
                if (vendor == null)
                {
                    return Json(new { success = false, message = "Vendor not found." });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        vendor.Id,
                        vendor.VendorCode,
                        vendor.VendorName,
                        vendor.PANNumber,
                        vendor.MobileNumber,
                        vendor.AlternateNumber,
                        vendor.EmailId,
                        vendor.Address,
                        vendor.AccountHolderName,
                        vendor.BankName,
                        vendor.BranchName,
                        vendor.AccountNumber,
                        vendor.IFSCCode,
                        vendor.PropertyAddress,
                        vendor.TotalRentAmount,
                        LinkedEmployees = vendor.LinkedEmployeesList,
                        vendor.Status,
                        ApprovalStatus = (int)vendor.ApprovalStatus,
                        vendor.ApprovalStatusText,
                        vendor.MakerUserName,
                        vendor.CheckerUserName,
                        MakerAction = (int)vendor.MakerAction,
                        MakerActionText = vendor.MakerAction.ToString(),
                        vendor.ApprovalDate,
                        vendor.RejectionReason,
                        vendor.CreatedDate,
                        vendor.UpdatedDate,
                        // Add backward compatibility for different property name cases
                        panNumber = vendor.PANNumber,
                        ifscCode = vendor.IFSCCode,
                        pANNumber = vendor.PANNumber,
                        iFSCCode = vendor.IFSCCode
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching vendor details for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while fetching vendor details." });
            }
        }

        // AJAX: Create vendor (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> CreateVendor([FromBody] VendorCreateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Check for duplicate vendor code
                var existingVendor = await _vendorRepository.GetVendorByCodeAsync(request.VendorCode);
                if (existingVendor != null)
                {
                    return Json(new { success = false, message = "Vendor code already exists. Please use a different vendor code." });
                }

                var vendor = new Vendor
                {
                    VendorCode = request.VendorCode,
                    VendorName = request.VendorName,
                    PANNumber = request.PanNumber,
                    MobileNumber = request.MobileNumber,
                    AlternateNumber = request.AlternateNumber,
                    EmailId = request.EmailId,
                    Address = request.Address,
                    AccountHolderName = request.AccountHolderName,
                    BankName = request.BankName,
                    BranchName = request.BranchName,
                    AccountNumber = request.AccountNumber,
                    IFSCCode = request.IfscCode,
                    PropertyAddress = request.PropertyAddress,
                    TotalRentAmount = request.TotalRentAmount,
                    LinkedEmployees = request.LinkedEmployees != null ? string.Join(",", request.LinkedEmployees) : null,
                    Status = request.Status
                };

                // Validate model
                if (!TryValidateModel(vendor))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                int vendorId;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly create approved vendors
                    vendor.ApprovalStatus = ApprovalStatus.Approved;
                    vendor.MakerUserId = userId;
                    vendor.MakerUserName = userName;
                    vendor.CheckerUserId = userId;
                    vendor.CheckerUserName = userName;
                    vendor.MakerAction = MakerAction.Create;
                    vendor.ApprovalDate = DateTime.Now;
                    vendorId = await _vendorRepository.AddVendorAsync(vendor);
                    message = "Vendor created successfully.";
                }
                else
                {
                    // Maker role - create vendor for approval
                    vendorId = await _vendorRepository.AddVendorForApprovalAsync(vendor, userId, userName, MakerAction.Create);
                    message = "Vendor created successfully and sent for approval.";
                }

                if (vendorId > 0)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create vendor." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating vendor");
                return Json(new { success = false, message = "An error occurred while creating the vendor." });
            }
        }

        // AJAX: Update vendor (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> UpdateVendor([FromBody] VendorUpdateRequest request)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var vendor = await _vendorRepository.GetVendorByIdAsync(request.Id);
                if (vendor == null)
                {
                    return Json(new { success = false, message = "Vendor not found." });
                }

                // Check if vendor has pending changes
                if (await _vendorRepository.HasPendingChangesAsync(request.Id))
                {
                    return Json(new { success = false, message = "This vendor has pending approval changes. Please wait for approval before making new changes." });
                }

                // Check for duplicate vendor code (excluding current vendor)
                var existingVendor = await _vendorRepository.GetVendorByCodeAsync(request.VendorCode);
                if (existingVendor != null && existingVendor.Id != request.Id)
                {
                    return Json(new { success = false, message = "Vendor code already exists. Please use a different vendor code." });
                }

                // Update vendor properties
                vendor.VendorCode = request.VendorCode;
                vendor.VendorName = request.VendorName;
                vendor.PANNumber = request.PanNumber;
                vendor.MobileNumber = request.MobileNumber;
                vendor.AlternateNumber = request.AlternateNumber;
                vendor.EmailId = request.EmailId;
                vendor.Address = request.Address;
                vendor.AccountHolderName = request.AccountHolderName;
                vendor.BankName = request.BankName;
                vendor.BranchName = request.BranchName;
                vendor.AccountNumber = request.AccountNumber;
                vendor.IFSCCode = request.IfscCode;
                vendor.PropertyAddress = request.PropertyAddress;
                vendor.TotalRentAmount = request.TotalRentAmount;
                vendor.LinkedEmployees = request.LinkedEmployees != null ? string.Join(",", request.LinkedEmployees) : null;
                vendor.Status = request.Status;

                // Validate model
                if (!TryValidateModel(vendor))
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly update approved vendors
                    vendor.CheckerUserId = userId;
                    vendor.CheckerUserName = userName;
                    vendor.ApprovalDate = DateTime.Now;
                    success = await _vendorRepository.UpdateVendorAsync(vendor);
                    message = "Vendor updated successfully.";
                }
                else
                {
                    // Maker role - update vendor for approval
                    success = await _vendorRepository.UpdateVendorForApprovalAsync(vendor, userId, userName);
                    message = "Vendor updated successfully and sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update vendor." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating vendor with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while updating the vendor." });
            }
        }

        // AJAX: Delete vendor (Maker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrEmployee)]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                // Check if vendor has pending changes
                if (await _vendorRepository.HasPendingChangesAsync(id))
                {
                    return Json(new { success = false, message = "This vendor has pending approval changes. Please wait for approval before making new changes." });
                }

                bool success;
                string message;

                if (userRole == UserRole.Admin)
                {
                    // Admin can directly delete vendors
                    success = await _vendorRepository.DeleteVendorAsync(id);
                    message = "Vendor deleted successfully.";
                }
                else
                {
                    // Maker role - mark vendor for deletion approval
                    success = await _vendorRepository.DeleteVendorForApprovalAsync(id, userId, userName);
                    message = "Vendor deletion request sent for approval.";
                }

                if (success)
                {
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete vendor." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting vendor with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while deleting the vendor." });
            }
        }

        // AJAX: Approve vendor (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> ApproveVendor(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                var success = await _vendorRepository.ApproveVendorAsync(id, userId, userName);
                if (success)
                {
                    return Json(new { success = true, message = "Vendor approved successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to approve vendor." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving vendor with ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while approving the vendor." });
            }
        }

        // AJAX: Reject vendor (Checker role)
        [HttpPost]
        [Authorize(Roles = Roles.AdminOrVendor)]
        public async Task<IActionResult> RejectVendor([FromBody] VendorRejectionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userName = GetCurrentUserName();

                if (string.IsNullOrEmpty(request.RejectionReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var success = await _vendorRepository.RejectVendorAsync(request.Id, userId, userName, request.RejectionReason);
                if (success)
                {
                    return Json(new { success = true, message = "Vendor rejected successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reject vendor." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rejecting vendor with ID: {Id}", request.Id);
                return Json(new { success = false, message = "An error occurred while rejecting the vendor." });
            }
        }

        // AJAX: Get vendors with pagination (for refresh after operations)
        [HttpGet]
        public async Task<IActionResult> GetVendors(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                IEnumerable<Vendor> vendors;
                int totalCount;

                if (userRole == UserRole.Checker || userRole == UserRole.Admin)
                {
                    if (string.IsNullOrEmpty(approvalStatusFilter) || approvalStatusFilter == "Approved")
                    {
                        vendors = await _vendorRepository.GetApprovedVendorsAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _vendorRepository.GetApprovedVendorCountAsync(searchTerm, statusFilter);
                    }
                    else if (approvalStatusFilter == "Pending")
                    {
                        vendors = await _vendorRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                        totalCount = await _vendorRepository.GetPendingApprovalCountAsync(searchTerm);
                    }
                    else if (approvalStatusFilter == "Rejected")
                    {
                        vendors = await _vendorRepository.GetRejectedVendorsAsync(searchTerm, page, pageSize);
                        totalCount = await _vendorRepository.GetRejectedVendorCountAsync(searchTerm);
                    }
                    else
                    {
                        vendors = await _vendorRepository.GetApprovedVendorsAsync(searchTerm, statusFilter, page, pageSize);
                        totalCount = await _vendorRepository.GetApprovedVendorCountAsync(searchTerm, statusFilter);
                    }
                }
                else
                {
                    vendors = await _vendorRepository.GetApprovedVendorsAsync(searchTerm, statusFilter, page, pageSize);
                    totalCount = await _vendorRepository.GetApprovedVendorCountAsync(searchTerm, statusFilter);
                }

                var result = new
                {
                    success = true,
                    data = vendors.Select(v => new
                    {
                        v.Id,
                        v.VendorCode,
                        v.VendorName,
                        v.MobileNumber,
                        v.IFSCCode,
                        v.BankName,
                        v.Status,
                        v.TotalRentAmount,
                        LinkedEmployees = v.LinkedEmployeesList,
                        ApprovalStatus = (int)v.ApprovalStatus,
                        ApprovalStatusText = v.ApprovalStatusText,
                        v.MakerUserName,
                        v.CheckerUserName,
                        MakerAction = (int)v.MakerAction,
                        MakerActionText = v.MakerAction.ToString(),
                        v.ApprovalDate,
                        v.RejectionReason,
                        v.CreatedDate,
                        v.UpdatedDate
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
                _logger.LogError(ex, "Error occurred while fetching vendors");
                return Json(new { success = false, message = "An error occurred while loading vendors." });
            }
        }

        // AJAX: Get employees for dropdown
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await _employeeRepository.GetAllEmployeesDropdownAsync();
                return Json(new { success = true, data = employees.ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employees");
                return Json(new { success = false, message = "An error occurred while loading employees." });
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
    }

    // Request models for AJAX operations
    public class VendorCreateRequest
    {
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string? AlternateNumber { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public decimal TotalRentAmount { get; set; }
        public List<string>? LinkedEmployees { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class VendorUpdateRequest : VendorCreateRequest
    {
        public int Id { get; set; }
    }

    public class VendorRejectionRequest
    {
        public int Id { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}