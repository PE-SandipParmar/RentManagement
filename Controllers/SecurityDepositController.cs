using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Threading.Tasks;

namespace RentManagement.Controllers
{
    public class SecurityDepositController : Controller
    {
        private readonly ISecurityDepositRepository _securityDepositRepository;

        public SecurityDepositController(ISecurityDepositRepository securityDepositRepository)
        {
            _securityDepositRepository = securityDepositRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var securityDeposits = await _securityDepositRepository.GetAllAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            return View(securityDeposits);
        }

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
            if (deposit.EmployeeId > 0 && deposit.Amount > 0)
            {
                var employeeSalary = await _securityDepositRepository.GetEmployeeSalaryAsync(deposit.EmployeeId);
                var maxAllowedAmount = employeeSalary * 2;

                if (deposit.Amount > maxAllowedAmount)
                {
                    ModelState.AddModelError("Amount", $"Security deposit cannot exceed ₹{maxAllowedAmount:N2} (HRA * 2 of ₹{employeeSalary:N2})");
                }
            }

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
            if (deposit.EmployeeId > 0 && deposit.Amount > 0)
            {
                var employeeSalary = await _securityDepositRepository.GetEmployeeSalaryAsync(deposit.EmployeeId);
                var maxAllowedAmount = employeeSalary * 2;

                if (deposit.Amount > maxAllowedAmount)
                {
                    ModelState.AddModelError("Amount", $"Security deposit cannot exceed ₹{maxAllowedAmount:N2} (2 × Monthly Salary of ₹{employeeSalary:N2})");
                }
            }

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
            var success = await _securityDepositRepository.DeleteAsync(id,0);
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

        // AJAX Methods for Enhanced Functionality

        /// <summary>
        /// Get employee salary for validation
        /// </summary>
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

        /// <summary>
        /// Get leases for specific employee
        /// </summary>
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

                var result = leases.Select(l => new {
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

        /// <summary>
        /// Get owner/vendor for specific lease
        /// </summary>
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

        /// <summary>
        /// Check for duplicate Employee + Lease + Owner combination
        /// </summary>
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
                    // For edit operations - exclude current record
                    isDuplicate = await _securityDepositRepository.IsDuplicateRecordAsync(employeeId, leaseId, vendorId, excludeId.Value);
                }
                else
                {
                    // For create operations
                    isDuplicate = await _securityDepositRepository.IsDuplicateRecordAsync(employeeId, leaseId, vendorId);
                }

                return Json(new { isDuplicate = isDuplicate });
            }
            catch (System.Exception)
            {
                // In case of error, assume not duplicate to allow operation
                return Json(new { isDuplicate = false });
            }
        }

        /// <summary>
        /// Validate security deposit amount
        /// </summary>
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

        /// <summary>
        /// Calculate TDS Amount based on Deposit Amount and TDS Rate
        /// </summary>
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

        private async Task LoadDropdowns()
        {
            ViewBag.Employees = await _securityDepositRepository.GetEmployeeNamesAsync();
            ViewBag.Vendors = await _securityDepositRepository.GetOwnersAsync();
            ViewBag.Leases = await _securityDepositRepository.GetLeaseNamesAsync();
        }
    }
}