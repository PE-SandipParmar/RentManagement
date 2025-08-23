using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System;

namespace RentManagement.Controllers
{
    public class LeaseController : Controller
    {
        private readonly ILeaseRepository _leaseRepository;

        public LeaseController(ILeaseRepository leaseRepository)
        {
            _leaseRepository = leaseRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var leases = await _leaseRepository.GetLeasesAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            // Load dropdowns for the drawer forms
            await LoadDropdowns();

            return View(leases);
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaseDetails(int id)
        {
            var lease = await _leaseRepository.GetLeaseByIdAsync(id);
            if (lease == null)
                return Json(new { success = false, message = "Lease not found" });

            return Json(new { success = true, data = lease });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Lease lease)
        {
            try
            {
                // Clear model state to avoid issues with JSON binding
                ModelState.Clear();

                // Manual validation for required fields
                var validationErrors = new Dictionary<string, string>();

                // Check for duplicate RefNo
                if (await _leaseRepository.LeaseNoExistsAsync(lease.RefNo))
                {
                    validationErrors.Add("RefNo", "This Lease Reference Number already exists. Please use a different one.");
                }

                // Validate required fields
                if (lease.LeaseTypeId <= 0)
                    validationErrors.Add("LeaseTypeId", "Lease Type is required");

                if (string.IsNullOrWhiteSpace(lease.RefNo))
                    validationErrors.Add("RefNo", "Lease Reference Number is required");

                if (lease.EmployeeId <= 0)
                    validationErrors.Add("EmployeeId", "Employee Name is required");

                if (!lease.RefDate.HasValue)
                    validationErrors.Add("RefDate", "Reference Date is required");

                if (lease.PerquisiteApplicablePercentId <= 0)
                    validationErrors.Add("PerquisiteApplicablePercentId", "% of Perquisite Applicable is required");

                if (lease.VendorId <= 0)
                    validationErrors.Add("VendorId", "Owner Name is required");

                if (!lease.MonthlyRentPayable.HasValue || lease.MonthlyRentPayable <= 0)
                    validationErrors.Add("MonthlyRentPayable", "Monthly Rent Payable must be greater than 0");

                if (!lease.FromDate.HasValue)
                    validationErrors.Add("FromDate", "From Date is required");

                if (!lease.EndDate.HasValue)
                    validationErrors.Add("EndDate", "End Date is required");

                if (lease.FromDate.HasValue && lease.EndDate.HasValue && lease.FromDate > lease.EndDate)
                    validationErrors.Add("EndDate", "End Date must be after From Date");

                if (lease.PaymentTermId <= 0)
                    validationErrors.Add("PaymentTermId", "Payment Term is required");

                if (lease.PayableOnOrBeforeId <= 0)
                    validationErrors.Add("PayableOnOrBeforeId", "Payable On or Before is required");

                if (string.IsNullOrWhiteSpace(lease.Narration))
                    validationErrors.Add("Narration", "Narration is required");
                else if (lease.Narration.Length > 200)
                    validationErrors.Add("Narration", "Narration cannot be longer than 200 characters");

                // Additional numeric field validations
                if (lease.RentDeposit.HasValue && lease.RentDeposit < 0)
                    validationErrors.Add("RentDeposit", "Rent Deposit cannot be negative");

                if (lease.AdditionalRentRecovery.HasValue && lease.AdditionalRentRecovery < 0)
                    validationErrors.Add("AdditionalRentRecovery", "Additional Rent Recovery cannot be negative");

                if (lease.BrokerageAmount.HasValue && lease.BrokerageAmount < 0)
                    validationErrors.Add("BrokerageAmount", "Brokerage Amount cannot be negative");

                if (lease.StampDuty.HasValue && lease.StampDuty < 0)
                    validationErrors.Add("StampDuty", "Stamp Duty cannot be negative");

                if (lease.LicenseFeeAmount.HasValue && lease.LicenseFeeAmount < 0)
                    validationErrors.Add("LicenseFeeAmount", "License Fee Amount cannot be negative");

                if (validationErrors.Any())
                {
                    return Json(new
                    {
                        success = false,
                        errors = validationErrors,
                        message = "Please correct the validation errors and try again."
                    });
                }

                var leaseId = await _leaseRepository.CreateLeaseAsync(lease);

                // Get the created lease with all details
                var createdLease = await _leaseRepository.GetLeaseByIdAsync(leaseId);

                return Json(new
                {
                    success = true,
                    message = "Lease created successfully!",
                    data = createdLease
                });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while creating the lease. Please try again or contact support if the problem persists.",
                    errors = new Dictionary<string, string> { { "General", ex.Message } }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAjax([FromBody] Lease lease)
        {
            try
            {
                // Check if lease exists
                var existingLease = await _leaseRepository.GetLeaseByIdAsync(lease.Id);
                if (existingLease == null)
                    return Json(new
                    {
                        success = false,
                        message = "Lease not found. It may have been deleted by another user."
                    });

                // Clear model state to avoid issues with JSON binding
                ModelState.Clear();

                // Manual validation for required fields
                var validationErrors = new Dictionary<string, string>();

                // Check for duplicate RefNo
                if (await _leaseRepository.LeaseNoExistsAsync(lease.RefNo, lease.Id))
                {
                    validationErrors.Add("RefNo", "This Lease Reference Number already exists. Please use a different one.");
                }

                // Validate required fields
                if (lease.LeaseTypeId <= 0)
                    validationErrors.Add("LeaseTypeId", "Lease Type is required");

                if (string.IsNullOrWhiteSpace(lease.RefNo))
                    validationErrors.Add("RefNo", "Lease Reference Number is required");

                if (lease.EmployeeId <= 0)
                    validationErrors.Add("EmployeeId", "Employee Name is required");

                if (!lease.RefDate.HasValue)
                    validationErrors.Add("RefDate", "Reference Date is required");

                if (lease.PerquisiteApplicablePercentId <= 0)
                    validationErrors.Add("PerquisiteApplicablePercentId", "% of Perquisite Applicable is required");

                if (lease.VendorId <= 0)
                    validationErrors.Add("VendorId", "Owner Name is required");

                if (!lease.MonthlyRentPayable.HasValue || lease.MonthlyRentPayable <= 0)
                    validationErrors.Add("MonthlyRentPayable", "Monthly Rent Payable must be greater than 0");

                if (!lease.FromDate.HasValue)
                    validationErrors.Add("FromDate", "From Date is required");

                if (!lease.EndDate.HasValue)
                    validationErrors.Add("EndDate", "End Date is required");

                if (lease.FromDate.HasValue && lease.EndDate.HasValue && lease.FromDate > lease.EndDate)
                    validationErrors.Add("EndDate", "End Date must be after From Date");

                if (lease.PaymentTermId <= 0)
                    validationErrors.Add("PaymentTermId", "Payment Term is required");

                if (lease.PayableOnOrBeforeId <= 0)
                    validationErrors.Add("PayableOnOrBeforeId", "Payable On or Before is required");

                if (string.IsNullOrWhiteSpace(lease.Narration))
                    validationErrors.Add("Narration", "Narration is required");
                else if (lease.Narration.Length > 200)
                    validationErrors.Add("Narration", "Narration cannot be longer than 200 characters");

                // Additional numeric field validations
                if (lease.RentDeposit.HasValue && lease.RentDeposit < 0)
                    validationErrors.Add("RentDeposit", "Rent Deposit cannot be negative");

                if (lease.AdditionalRentRecovery.HasValue && lease.AdditionalRentRecovery < 0)
                    validationErrors.Add("AdditionalRentRecovery", "Additional Rent Recovery cannot be negative");

                if (lease.BrokerageAmount.HasValue && lease.BrokerageAmount < 0)
                    validationErrors.Add("BrokerageAmount", "Brokerage Amount cannot be negative");

                if (lease.StampDuty.HasValue && lease.StampDuty < 0)
                    validationErrors.Add("StampDuty", "Stamp Duty cannot be negative");

                if (lease.LicenseFeeAmount.HasValue && lease.LicenseFeeAmount < 0)
                    validationErrors.Add("LicenseFeeAmount", "License Fee Amount cannot be negative");

                if (validationErrors.Any())
                {
                    return Json(new
                    {
                        success = false,
                        errors = validationErrors,
                        message = "Please correct the validation errors and try again."
                    });
                }

                var success = await _leaseRepository.UpdateLeaseAsync(lease);
                if (success)
                {
                    // Get the updated lease with all details
                    var updatedLease = await _leaseRepository.GetLeaseByIdAsync(lease.Id);

                    return Json(new
                    {
                        success = true,
                        message = "Lease updated successfully!",
                        data = updatedLease
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Failed to update lease. The lease may have been modified by another user. Please refresh and try again."
                });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while updating the lease. Please try again or contact support if the problem persists.",
                    errors = new Dictionary<string, string> { { "General", ex.Message } }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            try
            {
                var success = await _leaseRepository.DeleteLeaseAsync(id);
                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Lease deleted successfully!"
                    });
                }

                return Json(new { success = false, message = "Failed to delete lease." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the lease." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDropdownData()
        {
            var data = new
            {
                leaseTypes = await _leaseRepository.GetLeaseTypesAsync(),
                employeeNames = await _leaseRepository.GetEmployeeNamesAsync(),
                owners = await _leaseRepository.GetOwnersAsync(),
                rentRecoveryElements = await _leaseRepository.GetRentRecoveryElementsAsync(),
                licenseFeeRecoveryElements = await _leaseRepository.GetLicenseFeeRecoveryElementsAsync(),
                paymentTerms = await _leaseRepository.GetPaymentTermsAsync(),
                payableOnOrBeforeOptions = await _leaseRepository.GetPayableOnOrBeforeOptionsAsync(),
                perquisitePercents = await _leaseRepository.GetPerquisiteApplicablePercentsAsync()
            };

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetTableData(int page = 1, int pageSize = 10, string search = "")
        {
            var leases = await _leaseRepository.GetLeasesAsync(page, pageSize, search);
            return PartialView("_LeaseTablePartial", leases);
        }

        private async Task LoadDropdowns()
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
    }
}