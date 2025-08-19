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
            var success = await _securityDepositRepository.DeleteAsync(id);
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
        private async Task LoadDropdowns()
        {
            ViewBag.Employees = await _securityDepositRepository.GetEmployeeNamesAsync();
            ViewBag.Vendors = await _securityDepositRepository.GetOwnersAsync();
            ViewBag.Leases = await _securityDepositRepository.GetLeaseNamesAsync();
        }
    }
}
