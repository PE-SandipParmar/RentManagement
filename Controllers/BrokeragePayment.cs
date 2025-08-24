using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RentManagement.Controllers
{

    public class BrokeragePaymentController : Controller
    {
        private readonly IBrokeragePaymentRepository _BrokeragePaymentRepository;

        public BrokeragePaymentController(IBrokeragePaymentRepository BrokeragePaymentRepository)
        {
            _BrokeragePaymentRepository = BrokeragePaymentRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var BrokeragePayment = await _BrokeragePaymentRepository.GetAllAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            return View(BrokeragePayment);
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _BrokeragePaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // Updated Create and Edit actions in BrokeragePaymentController

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrokeragePayment payment)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validate business rules before database operation
                    var validationResult = await ValidateBrokeragePaymentRules(payment);
                    if (!validationResult.IsValid)
                    {
                        ModelState.AddModelError("BrokerageAmount", validationResult.ErrorMessage);
                        await LoadDropdowns();
                        return View(payment);
                    }

                    var newId = await _BrokeragePaymentRepository.CreateAsync(payment);
                    TempData["SuccessMessage"] = "Brokerage Payment created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (SqlException ex) when (ex.Message.Contains("Brokerage amount") && ex.Message.Contains("exceeds maximum"))
            {
                // Handle the specific brokerage validation error
                var errorMsg = ex.Message;
                ModelState.AddModelError("BrokerageAmount", errorMsg);
            }
            catch (SqlException ex) when (ex.Message.Contains("already exists"))
            {
                // Handle duplicate brokerage payment error
                ModelState.AddModelError("", "Brokerage payment already exists for this employee-lease combination.");
            }
            catch (Exception ex)
            {
                // Handle other errors
                ModelState.AddModelError("", "An error occurred while creating the brokerage payment. Please try again.");
                // Log the exception here if you have logging configured
            }

            await LoadDropdowns();
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BrokeragePayment payment)
        {
            if (id != payment.Id)
                return NotFound();

            try
            {
                if (ModelState.IsValid)
                {
                    // Validate business rules before database operation
                    var validationResult = await ValidateBrokeragePaymentRules(payment);
                    if (!validationResult.IsValid)
                    {
                        ModelState.AddModelError("BrokerageAmount", validationResult.ErrorMessage);
                        await LoadDropdowns();
                        return View(payment);
                    }

                    var success = await _BrokeragePaymentRepository.UpdateAsync(payment);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Brokerage Payment updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update Brokerage Payment.";
                    }
                }
            }
            catch (SqlException ex) when (ex.Message.Contains("Brokerage amount") && ex.Message.Contains("exceeds maximum"))
            {
                var errorMsg = ex.Message;
                ModelState.AddModelError("BrokerageAmount", errorMsg);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the brokerage payment. Please try again.");
            }

            await LoadDropdowns();
            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> ValidateBrokerageAmount(int employeeId, int leaseId, decimal brokerageAmount, int? excludeId = null)
        {
            try
            {
                // Validate input parameters
                if (employeeId <= 0 || leaseId <= 0)
                {
                    return Json(new
                    {
                        isValid = false,
                        errorMessage = "Please select both employee and lease.",
                        employeeSalary = 0,
                        leaseMaxBrokerage = 0,
                        maxAllowed = 0
                    });
                }

                if (brokerageAmount <= 0)
                {
                    return Json(new
                    {
                        isValid = false,
                        errorMessage = "Brokerage amount must be greater than zero.",
                        employeeSalary = 0,
                        leaseMaxBrokerage = 0,
                        maxAllowed = 0
                    });
                }

                // Get employee salary
                    var employeeSalary = await _BrokeragePaymentRepository.GetEmployeeSalaryAsync(employeeId);

                // Get lease details including max brokerage
                var leaseDetails = await _BrokeragePaymentRepository.GetLeaseDetailsAsync(leaseId);

                if (leaseDetails == null)
                {
                    return Json(new
                    {
                        isValid = false,
                        errorMessage = "Lease details not found.",
                        employeeSalary = employeeSalary,
                        leaseMaxBrokerage = 0,
                        maxAllowed = 0
                    });
                }

                var leaseMaxBrokerage = leaseDetails.MaxBrokerageAmount;

                // Calculate maximum allowed brokerage (higher of employee salary or lease max)
                var maxAllowed = Math.Max(employeeSalary, leaseMaxBrokerage);

                // Check if brokerage amount exceeds the limit
                if (brokerageAmount > maxAllowed)
                {
                    return Json(new
                    {
                        isValid = false,
                        errorMessage = $" ₹{brokerageAmount:N2} this brokerage amount should not exceed the lease brokerage maximum amount of ₹{maxAllowed:N2}.",
                        employeeSalary = employeeSalary,
                        leaseMaxBrokerage = leaseMaxBrokerage,
                        maxAllowed = maxAllowed
                    });
                }

                // Check for existing brokerage payment (one-time payment rule)
                var existingPayment = await _BrokeragePaymentRepository.CheckExistingBrokerageAsync(employeeId, leaseId);

                if (existingPayment != null && (excludeId == null || existingPayment.Id != excludeId))
                {
                    return Json(new
                    {
                        isValid = false,
                        errorMessage = $"Brokerage payment already exists for this employee-lease combination. Payment was made on {existingPayment.PaymentMonth:MMM yyyy} for ₹{existingPayment.BrokerageAmount:N2}.",
                        employeeSalary = employeeSalary,
                        leaseMaxBrokerage = leaseMaxBrokerage,
                        maxAllowed = maxAllowed
                    });
                }

                // All validations passed
                return Json(new
                {
                    isValid = true,
                    errorMessage = string.Empty,
                    employeeSalary = employeeSalary,
                    leaseMaxBrokerage = leaseMaxBrokerage,
                    maxAllowed = maxAllowed,
                    message = $"Valid brokerage amount within limit of ₹{maxAllowed:N2}"
                });
            }
            catch (Exception ex)
            {
                // Log the exception if you have logging configured
                // _logger.LogError(ex, "Error validating brokerage amount for Employee: {EmployeeId}, Lease: {LeaseId}", employeeId, leaseId);

                return Json(new
                {
                    isValid = false,
                    errorMessage = "An error occurred while validating the brokerage amount. Please try again.",
                    employeeSalary = 0,
                    leaseMaxBrokerage = 0,
                    maxAllowed = 0
                });
            }
        }

     
        // Add this new validation method
        private async Task<ValidationResult> ValidateBrokeragePaymentRules(BrokeragePayment payment)
        {
            try
            {
                // Get employee salary
                var employeeSalary = await _BrokeragePaymentRepository.GetEmployeeSalaryAsync(payment.EmployeeId);

                // Get lease max brokerage
                var leaseDetails = await _BrokeragePaymentRepository.GetLeaseDetailsAsync(payment.LeaseId);
                var leaseMaxBrokerage = leaseDetails?.MaxBrokerageAmount ?? 0;

                // Calculate maximum allowed
                var maxAllowed = Math.Max(employeeSalary, leaseMaxBrokerage);

                // Check if brokerage amount exceeds limit
                if (payment.BrokerageAmount > maxAllowed)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Brokerage amount (₹{payment.BrokerageAmount:N2}) exceeds maximum allowed (₹{maxAllowed:N2}). " +
                                     $"Maximum is based on Employee Salary: ₹{employeeSalary:N2} or Lease Max: ₹{leaseMaxBrokerage:N2}"
                    };
                }

                // Check for existing brokerage payment
                var existingPayment = await _BrokeragePaymentRepository.CheckExistingBrokerageAsync(payment.EmployeeId, payment.LeaseId);
                if (existingPayment != null && existingPayment.Id != payment.Id)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Brokerage payment already exists for this employee-lease combination."
                    };
                }

                return new ValidationResult { IsValid = true };
            }
            catch (Exception)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Unable to validate brokerage payment rules. Please try again."
                };
            }
        }

        // Add this helper class
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(BrokeragePayment payment)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var newId = await _BrokeragePaymentRepository.CreateAsync(payment);
        //        TempData["SuccessMessage"] = "Brokerage Payement created successfully!";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    await LoadDropdowns();

        //    return View(payment);
        //}

        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _BrokeragePaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();
            await LoadDropdowns();

            return View(payment);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, BrokeragePayment payment)
        //{
        //    if (id != payment.Id)
        //        return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        var success = await _BrokeragePaymentRepository.UpdateAsync(payment);
        //        if (success)
        //        {
        //            TempData["SuccessMessage"] = "Brokerage updated successfully!";
        //            return RedirectToAction(nameof(Index));
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "Failed to update Brokerage Payment.";
        //        }
        //    }
        //    await LoadDropdowns();

        //    return View(payment);
        //}
     

        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _BrokeragePaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _BrokeragePaymentRepository.DeleteAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Brokerage Payment deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete Brokerage Payment.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _BrokeragePaymentRepository.ToggleActiveStatus(id);
            return RedirectToAction(nameof(Index));
        }
        private async Task LoadDropdowns()
        {

            ViewBag.Leases = await _BrokeragePaymentRepository.GetLeaseNameAsync();
            ViewBag.Employees = await _BrokeragePaymentRepository.GetEmployeeNamesAsync();
            ViewBag.Vendors = await _BrokeragePaymentRepository.GetOwnersAsync();
            ViewBag.TDSApplicable = await _BrokeragePaymentRepository.GetTdsApplicableAsync();
        }

        // Add these methods to your BrokeragePaymentController class

        [HttpGet]
        public async Task<IActionResult> GetEmployeeLeases(int employeeId)
        {
            try
            {
                var leases = await _BrokeragePaymentRepository.GetLeasesByEmployeeAsync(employeeId);
                return Json(leases.Select(l => new { id = l.Id, name = l.Name }));
            }
            catch (Exception ex)
            {
                return Json(new { error = "Failed to load leases" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeSalary(int employeeId)
        {
            try
            {
                var salary = await _BrokeragePaymentRepository.GetEmployeeSalaryAsync(employeeId);
                return Json(new { salary = salary });
            }
            catch (Exception ex)
            {
                return Json(new { salary = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaseDetails(int leaseId)
        {
            try
            {
                var leaseDetails = await _BrokeragePaymentRepository.GetLeaseDetailsAsync(leaseId);
                return Json(new
                {
                    vendorId = leaseDetails.VendorId,
                    vendorName = leaseDetails.VendorName,
                    maxBrokerageAmount = leaseDetails.MaxBrokerageAmount,
                    monthlyRent = leaseDetails.MonthlyRent
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Failed to load lease details" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateBrokeragePayment(int employeeId, int leaseId)
        {
            try
            {
                // Check if brokerage already paid for this employee-lease combination
                var existingPayment = await _BrokeragePaymentRepository.CheckExistingBrokerageAsync(employeeId, leaseId);

                return Json(new
                {
                    alreadyPaid = existingPayment != null,
                    message = existingPayment != null ? "Brokerage already paid for this employee-lease combination" : "Valid"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Failed to validate" });
            }
        }
    }

}


