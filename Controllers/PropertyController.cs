using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Security.Claims;

namespace RentManagement.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyRepository propertyRepository,
            IVendorRepository vendorRepository,
            IEmployeeRepository employeeRepository,
            ILogger<PropertyController> logger)
        {
            _propertyRepository = propertyRepository;
            _vendorRepository = vendorRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        #region Private Helper Methods

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
        }

        private string GetCurrentUserName()
        {
            return User.FindFirstValue(ClaimTypes.Name) ?? "System User";
        }

        private UserRole GetCurrentUserRole()
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Maker";
            return Enum.Parse<UserRole>(role);
        }

        private bool IsChecker()
        {
            var role = GetCurrentUserRole();
            return role == UserRole.Checker || role == UserRole.Admin;
        }

        private bool IsMaker()
        {
            var role = GetCurrentUserRole();
            return role == UserRole.Maker || role == UserRole.Admin;
        }

        #endregion

        #region Views

        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int? vendorFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var isChecker = IsChecker();

                // Get properties based on approval status filter
                IEnumerable<Property> properties;
                int totalRecords;

                if (approvalStatusFilter == "Pending")
                {
                    properties = await _propertyRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                    totalRecords = await _propertyRepository.GetPendingApprovalCountAsync(searchTerm);
                }
                else if (approvalStatusFilter == "Approved")
                {
                    properties = await _propertyRepository.GetApprovedPropertiesAsync(searchTerm, statusFilter, vendorFilter, page, pageSize);
                    totalRecords = await _propertyRepository.GetApprovedPropertyCountAsync(searchTerm, statusFilter, vendorFilter);
                }
                else if (approvalStatusFilter == "Rejected")
                {
                    properties = await _propertyRepository.GetRejectedPropertiesAsync(searchTerm, page, pageSize);
                    totalRecords = await _propertyRepository.GetRejectedPropertyCountAsync(searchTerm);
                }
                else
                {
                    properties = await _propertyRepository.GetAllPropertiesWithApprovalStatusAsync(searchTerm, statusFilter, vendorFilter, page, pageSize);
                    totalRecords = await _propertyRepository.GetAllPropertiesWithApprovalStatusCountAsync(searchTerm, statusFilter, vendorFilter);
                }

                // Get pending approvals for checkers
                var pendingApprovals = new List<Property>();
                if (isChecker)
                {
                    pendingApprovals = (await _propertyRepository.GetPendingApprovalsAsync("", 1, 10)).ToList();
                }

                // Get all approved vendors for filter dropdown
                var vendors = await _vendorRepository.GetApprovedVendorsAsync("", "", 1, 100);

                // Get employees for the view
                var employees = await _employeeRepository.GetAllEmployeesDropdownAsync();
                ViewBag.Employees = employees.ToList();

                var viewModel = new PropertyListViewModel
                {
                    Properties = properties.ToList(),
                    PendingApprovals = pendingApprovals,
                    Vendors = vendors.ToList(),
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    ApprovalStatusFilter = approvalStatusFilter,
                    VendorFilter = vendorFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    CurrentUserRole = userRole,
                    ShowApprovalSection = isChecker && pendingApprovals.Any()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading properties");
                TempData["Error"] = "An error occurred while loading properties.";
                return View(new PropertyListViewModel());
            }
        }

        #endregion

        #region AJAX Methods

        [HttpGet]
        public async Task<IActionResult> GetEmployeesNotLinkedToVendor(int vendorId, int? currentPropertyId = null)
        {
            try
            {
                if (vendorId <= 0)
                {
                    return Json(new { success = false, message = "Invalid vendor id." });
                }

                //var vendorProperties = await _propertyRepository.GetPropertiesByVendorAsync(vendorId);
                //var linkedEmployeeIds = new HashSet<int>();
                //foreach (var prop in vendorProperties)
                //{
                //    foreach (var empId in prop.LinkedEmployeesList)
                //    {
                //        linkedEmployeeIds.Add(empId);
                //    }
                //}

                //var allEmployees = await _employeeRepository.GetAllEmployeesDropdownAsync();
                //var available = allEmployees
                //    .Where(e => e.Id.HasValue)
                //    .Where(e => e.ApprovalStatus == ApprovalStatus.Approved)
                //    .Where(e => !linkedEmployeeIds.Contains(e.Id!.Value))
                //    .Select(e => new { id = e.Id!.Value, code = e.Code ?? string.Empty, name = e.Name })
                //    .OrderBy(e => e.name)
                //    .ToList();
                var allEmployees = await _employeeRepository.GetAllEmployeesDropdownAsync();
                var linkedEmployeeIds = new HashSet<int>();
                var allProperties = await _propertyRepository.GetAllPropertiesAsync();

                foreach (var property in allProperties.Where(e => e.ApprovalStatus == ApprovalStatus.Approved))
                {
                    if (currentPropertyId.HasValue && property.Id == currentPropertyId.Value)
                    {
                        // Skip excluding employees already linked to the property we are editing
                        continue;
                    }
                    if (!string.IsNullOrEmpty(property.LinkedEmployees))
                    {
                        var employeeIds = property.LinkedEmployees.Split(',')
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => int.TryParse(x.Trim(), out int id) ? id : 0)
                            .Where(id => id > 0);

                        foreach (var id in employeeIds)
                        {
                            linkedEmployeeIds.Add(id);
                        }
                    }
                }

                var availableEmployees = allEmployees.Where(e => e.Id.HasValue &&  !linkedEmployeeIds.Contains(e.Id.Value)).ToList();

                var employeeCount = availableEmployees.Count;

               var available = availableEmployees.Select(e => new
                {
                    id = e.Id.Value,
                    name = e.Name,
                   code =  e.Code
                }).ToList();
                return Json(new { success = true, data = available });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees not linked to vendor {VendorId}", vendorId);
                return Json(new { success = false, message = "An error occurred while loading employees." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProperties(string searchTerm = "", string statusFilter = "", string approvalStatusFilter = "", int? vendorFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                IEnumerable<Property> properties;
                int totalRecords;

                if (approvalStatusFilter == "Pending")
                {
                    properties = await _propertyRepository.GetPendingApprovalsAsync(searchTerm, page, pageSize);
                    totalRecords = await _propertyRepository.GetPendingApprovalCountAsync(searchTerm);
                }
                else if (approvalStatusFilter == "Approved")
                {
                    properties = await _propertyRepository.GetApprovedPropertiesAsync(searchTerm, statusFilter, vendorFilter, page, pageSize);
                    totalRecords = await _propertyRepository.GetApprovedPropertyCountAsync(searchTerm, statusFilter, vendorFilter);
                }
                else if (approvalStatusFilter == "Rejected")
                {
                    properties = await _propertyRepository.GetRejectedPropertiesAsync(searchTerm, page, pageSize);
                    totalRecords = await _propertyRepository.GetRejectedPropertyCountAsync(searchTerm);
                }
                else
                {
                    properties = await _propertyRepository.GetAllPropertiesWithApprovalStatusAsync(searchTerm, statusFilter, vendorFilter, page, pageSize);
                    totalRecords = await _propertyRepository.GetAllPropertiesWithApprovalStatusCountAsync(searchTerm, statusFilter, vendorFilter);
                }

                var propertyList = new List<object>();
                foreach (var p in properties)
                {
                    var employeeNames = await GetEmployeeNamesAsync(p.LinkedEmployeesList);
                    propertyList.Add(new
                    {
                        id = p.Id,
                        propertyCode = p.PropertyCode,
                        vendorId = p.VendorId,
                        vendorCode = p.VendorCode,
                        vendorName = p.VendorName,
                        propertyAddress = p.PropertyAddress,
                        totalRentAmount = p.TotalRentAmount,
                        linkedEmployees = p.LinkedEmployeesList,
                        linkedEmployeeNames = employeeNames,
                        status = p.Status,
                        approvalStatus = (int)p.ApprovalStatus,
                        approvalStatusText = p.ApprovalStatusText,
                        makerUserName = p.MakerUserName,
                        makerAction = p.MakerActionText,
                        brokerId = p.BrokerId,
                        brokerCode = p.BrokerCode,
                        brokerName = p.BrokerName
                    });
                }

                return Json(new
                {
                    success = true,
                    data = propertyList,
                    pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalRecords = totalRecords,
                        TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties");
                return Json(new { success = false, message = "An error occurred while loading properties." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPropertyDetails(int id)
        {
            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(id);
                if (property == null)
                {
                    return Json(new { success = false, message = "Property not found." });
                }

                var employeeNames = await GetEmployeeNamesAsync(property.LinkedEmployeesList);
                
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = property.Id,
                        propertyCode = property.PropertyCode,
                        vendorId = property.VendorId,
                        vendorCode = property.VendorCode,
                        vendorName = property.VendorName,
                        propertyAddress = property.PropertyAddress,
                        totalRentAmount = property.TotalRentAmount,
                        linkedEmployees = property.LinkedEmployeesList,
                        linkedEmployeeNames = employeeNames,
                        status = property.Status,
                        approvalStatus = property.ApprovalStatus,
                        approvalStatusText = property.ApprovalStatusText,
                        makerUserName = property.MakerUserName,
                        checkerUserName = property.CheckerUserName,
                        rejectionReason = property.RejectionReason,
                        approvalDate = property.ApprovalDate?.ToString("dd-MM-yyyy"),
                        brokerId = property.BrokerId,
                        brokerCode = property.BrokerCode,
                        brokerName = property.BrokerName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property details for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while loading property details." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNextPropertyCode()
        {
            try
            {
                var propertyCode = await _propertyRepository.GetNextPropertyCodeAsync();
                return Json(new { success = true, propertyCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating property code");
                return Json(new { success = false, message = "An error occurred while generating property code." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovedVendors()
        {
            try
            {
                var vendors = await _vendorRepository.GetApprovedVendorsAsync("", "", 1, 1000);
                var vendorList = vendors.Select(v => new
                {
                    id = v.Id,
                    vendorCode = v.VendorCode,
                    vendorName = v.VendorName
                }).ToList();

                return Json(new { success = true, data = vendorList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approved vendors");
                return Json(new { success = false, message = "An error occurred while loading vendors." });
            }
        }

        #endregion

        #region CRUD Operations

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProperty([FromBody] Property model)
        {
            try
            {
                if (!IsMaker())
                {
                    return Json(new { success = false, message = "You don't have permission to create properties." });
                }

                // Validate the model
                var errors = ValidateProperty(model);
                if (errors.Any())
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                // Generate property code
                model.PropertyCode = await _propertyRepository.GetNextPropertyCodeAsync();

                // Set linked employees
                if (model.LinkedEmployeesList != null && model.LinkedEmployeesList.Any())
                {
                    model.LinkedEmployees = string.Join(",", model.LinkedEmployeesList);
                }

                // Add property for approval
                var propertyId = await _propertyRepository.AddPropertyForApprovalAsync(
                    model,
                    GetCurrentUserId(),
                    GetCurrentUserName(),
                    MakerAction.Create
                );

                if (propertyId > 0)
                {
                    return Json(new { success = true, message = "Property created successfully and sent for approval." });
                }

                return Json(new { success = false, message = "Failed to create property." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                return Json(new { success = false, message = "An error occurred while creating the property." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProperty([FromBody] Property model)
        {
            try
            {
                if (!IsMaker())
                {
                    return Json(new { success = false, message = "You don't have permission to update properties." });
                }

                // Check if property exists
                var existingProperty = await _propertyRepository.GetPropertyByIdAsync(model.Id);
                if (existingProperty == null)
                {
                    return Json(new { success = false, message = "Property not found." });
                }

                // Check if there are pending changes
                if (await _propertyRepository.HasPendingChangesAsync(model.Id))
                {
                    return Json(new { success = false, message = "This property has pending changes awaiting approval." });
                }

                // Validate the model
                var errors = ValidateProperty(model);
                if (errors.Any())
                {
                    return Json(new { success = false, message = "Validation failed.", errors });
                }

                // Set linked employees
                if (model.LinkedEmployeesList != null && model.LinkedEmployeesList.Any())
                {
                    model.LinkedEmployees = string.Join(",", model.LinkedEmployeesList);
                }

                // Update property for approval
                var result = await _propertyRepository.UpdatePropertyForApprovalAsync(
                    model,
                    GetCurrentUserId(),
                    GetCurrentUserName()
                );

                if (result)
                {
                    return Json(new { success = true, message = "Property updated successfully and sent for approval." });
                }

                return Json(new { success = false, message = "Failed to update property." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property");
                return Json(new { success = false, message = "An error occurred while updating the property." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                if (!IsMaker())
                {
                    return Json(new { success = false, message = "You don't have permission to delete properties." });
                }

                // Check if property exists
                var property = await _propertyRepository.GetPropertyByIdAsync(id);
                if (property == null)
                {
                    return Json(new { success = false, message = "Property not found." });
                }

                // Mark for deletion approval
                var result = await _propertyRepository.DeletePropertyForApprovalAsync(
                    id,
                    GetCurrentUserId(),
                    GetCurrentUserName()
                );

                if (result)
                {
                    return Json(new { success = true, message = "Property deletion request sent for approval." });
                }

                return Json(new { success = false, message = "Failed to delete property." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property");
                return Json(new { success = false, message = "An error occurred while deleting the property." });
            }
        }

        #endregion

        #region Approval Workflow

        [HttpPost]
        public async Task<IActionResult> ApproveProperty(int id)
        {
            try
            {
                if (!IsChecker())
                {
                    return Json(new { success = false, message = "You don't have permission to approve properties." });
                }

                var result = await _propertyRepository.ApprovePropertyAsync(
                    id,
                    GetCurrentUserId(),
                    GetCurrentUserName()
                );

                if (result)
                {
                    return Json(new { success = true, message = "Property approved successfully." });
                }

                return Json(new { success = false, message = "Failed to approve property." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving property");
                return Json(new { success = false, message = "An error occurred while approving the property." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectProperty([FromBody] RejectRequest request)
        {
            try
            {
                if (!IsChecker())
                {
                    return Json(new { success = false, message = "You don't have permission to reject properties." });
                }

                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    return Json(new { success = false, message = "Rejection reason is required." });
                }

                var result = await _propertyRepository.RejectPropertyAsync(
                    request.Id,
                    GetCurrentUserId(),
                    GetCurrentUserName(),
                    request.RejectionReason
                );

                if (result)
                {
                    return Json(new { success = true, message = "Property rejected successfully." });
                }

                return Json(new { success = false, message = "Failed to reject property." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting property");
                return Json(new { success = false, message = "An error occurred while rejecting the property." });
            }
        }

        #endregion

        #region Private Validation Methods

        private List<string> ValidateProperty(Property property)
        {
            var errors = new List<string>();

            if (property.VendorId <= 0)
            {
                errors.Add("Please select a valid owner.");
            }

            if (string.IsNullOrWhiteSpace(property.PropertyAddress))
            {
                errors.Add("Property Address is required.");
            }
            else if (property.PropertyAddress.Length > 500)
            {
                errors.Add("Property Address cannot exceed 500 characters.");
            }

            //if (property.TotalRentAmount <= 0)
            //{
            //    errors.Add("Total Rent Amount must be greater than 0.");
            //}

            return errors;
        }

        #endregion

        #region Private Helper Methods

        private async Task<List<string>> GetEmployeeNamesAsync(List<int> employeeIds)
        {
            if (employeeIds == null || !employeeIds.Any())
                return new List<string>();

            try
            {

                var employees = await _employeeRepository.GetEmployeesByIdsAsync(employeeIds);
                return employees.Select(e => e.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee names for IDs: {EmployeeIds}", string.Join(",", employeeIds));
                return new List<string>();
            }
        }

        #endregion

        public class RejectRequest
        {
            public int Id { get; set; }
            public string RejectionReason { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetApprovedBrokers()
        {
            try
            {
                var brokers = await _vendorRepository.GetApprovedBrokersAsync("", "", 1, 1000);
                var brokerList = brokers.Select(b => new
                {
                    id = b.Id,
                    vendorCode = b.VendorCode,
                    vendorName = b.VendorName
                }).ToList();

                return Json(new { success = true, data = brokerList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approved brokers");
                return Json(new { success = false, message = "An error occurred while loading brokers." });
            }
        }
    }
}