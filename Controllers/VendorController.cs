using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using static RentManagement.Models.Vendor;

namespace RentManagement.Controllers
{
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
        public async Task<IActionResult> Index(string searchTerm = "", string statusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var vendors = await _vendorRepository.SearchVendorsAsync(searchTerm, statusFilter, page, pageSize);
                var totalCount = await _vendorRepository.GetVendorCountAsync(searchTerm, statusFilter);
                var employees = await _employeeRepository.GetAllEmployeesDropdownAsync();

                var viewModel = new VendorListViewModel
                {
                    Vendors = vendors.ToList(),
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalCount
                };

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
                        vendor.CreatedDate,
                        vendor.UpdatedDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching vendor details for ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while fetching vendor details." });
            }
        }

        // AJAX: Create vendor
        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] VendorCreateRequest request)
        {
            try
            {
                var vendor = new Vendor
                {
                    VendorCode = request.VendorCode,
                    VendorName = request.VendorName,
                    PANNumber = request.PANNumber,
                    MobileNumber = request.MobileNumber,
                    AlternateNumber = request.AlternateNumber,
                    EmailId = request.EmailId,
                    Address = request.Address,
                    AccountHolderName = request.AccountHolderName,
                    BankName = request.BankName,
                    BranchName = request.BranchName,
                    AccountNumber = request.AccountNumber,
                    IFSCCode = request.IFSCCode,
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

                var vendorId = await _vendorRepository.AddVendorAsync(vendor);
                if (vendorId > 0)
                {
                    return Json(new { success = true, message = "Vendor created successfully." });
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

        // AJAX: Update vendor
        [HttpPost]
        public async Task<IActionResult> UpdateVendor([FromBody] VendorUpdateRequest request)
        {
            try
            {
                var vendor = await _vendorRepository.GetVendorByIdAsync(request.Id);
                if (vendor == null)
                {
                    return Json(new { success = false, message = "Vendor not found." });
                }

                // Update vendor properties
                vendor.VendorCode = request.VendorCode;
                vendor.VendorName = request.VendorName;
                vendor.PANNumber = request.PANNumber;
                vendor.MobileNumber = request.MobileNumber;
                vendor.AlternateNumber = request.AlternateNumber;
                vendor.EmailId = request.EmailId;
                vendor.Address = request.Address;
                vendor.AccountHolderName = request.AccountHolderName;
                vendor.BankName = request.BankName;
                vendor.BranchName = request.BranchName;
                vendor.AccountNumber = request.AccountNumber;
                vendor.IFSCCode = request.IFSCCode;
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

                var success = await _vendorRepository.UpdateVendorAsync(vendor);
                if (success)
                {
                    return Json(new { success = true, message = "Vendor updated successfully." });
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

        // AJAX: Delete vendor
        [HttpPost]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            try
            {
                var success = await _vendorRepository.DeleteVendorAsync(id);
                if (success)
                {
                    return Json(new { success = true, message = "Vendor deleted successfully." });
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

        // AJAX: Get vendors with pagination (for refresh after operations)
        [HttpGet]
        public async Task<IActionResult> GetVendors(string searchTerm = "", string statusFilter = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var vendors = await _vendorRepository.SearchVendorsAsync(searchTerm, statusFilter, page, pageSize);
                var totalCount = await _vendorRepository.GetVendorCountAsync(searchTerm, statusFilter);

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
                        LinkedEmployees = v.LinkedEmployeesList
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
    }

    // Request models for AJAX operations
    public class VendorCreateRequest
    {
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string PANNumber { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string? AlternateNumber { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IFSCCode { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public decimal TotalRentAmount { get; set; }
        public List<string>? LinkedEmployees { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class VendorUpdateRequest : VendorCreateRequest
    {
        public int Id { get; set; }
    }
}
