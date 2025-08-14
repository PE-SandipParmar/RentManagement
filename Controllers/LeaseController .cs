using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Threading.Tasks;

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

            return View(leases);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lease = await _leaseRepository.GetLeaseByIdAsync(id);
            if (lease == null)
                return NotFound();

            return View(lease);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lease lease)
        {
            if (ModelState.IsValid)
            {
                var leaseId = await _leaseRepository.CreateLeaseAsync(lease);
                TempData["SuccessMessage"] = "Lease created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(lease);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var lease = await _leaseRepository.GetLeaseByIdAsync(id);
            if (lease == null)
                return NotFound();

            await LoadDropdowns();
            return View(lease);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lease lease)
        {
            if (id != lease.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var success = await _leaseRepository.UpdateLeaseAsync(lease);
                if (success)
                {
                    TempData["SuccessMessage"] = "Lease updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update lease.";
                }
            }

            await LoadDropdowns();
            return View(lease);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var lease = await _leaseRepository.GetLeaseByIdAsync(id);
            if (lease == null)
                return NotFound();

            return View(lease);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _leaseRepository.DeleteLeaseAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Lease deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete lease.";
            }

            return RedirectToAction(nameof(Index));
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
