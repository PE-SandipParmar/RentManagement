using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Threading.Tasks;

namespace RentManagement.Controllers
{

    public class MonthlyRentPaymentController : Controller
    {
        private readonly IMonthlyRentPaymentRepository _monthlyRentPaymentRepository;

        public MonthlyRentPaymentController(IMonthlyRentPaymentRepository monthlyRentPaymentRepository)
        {
            _monthlyRentPaymentRepository = monthlyRentPaymentRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var MonthlyRentPayment = await _monthlyRentPaymentRepository.GetAllAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            return View(MonthlyRentPayment);
        }

        // View monthly rent payment details
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _monthlyRentPaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        // Create monthly rent payment (GET)
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // Create monthly rent payment (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonthlyRentPayment payment)
        {
            if (ModelState.IsValid)
            {
                var newId = await _monthlyRentPaymentRepository.CreateAsync(payment);
                TempData["SuccessMessage"] = "Monthly rent payment created successfully!";
                return RedirectToAction(nameof(Index));
            }
            await LoadDropdowns();

            return View(payment);
        }

        // Edit monthly rent payment (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _monthlyRentPaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();
            await LoadDropdowns();

            return View(payment);
        }

        // Edit monthly rent payment (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonthlyRentPayment payment)
        {
            if (id != payment.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var success = await _monthlyRentPaymentRepository.UpdateAsync(payment);
                if (success)
                {
                    TempData["SuccessMessage"] = "Monthly rent payment updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update monthly rent payment.";
                }
            }
            await LoadDropdowns();

            return View(payment);
        }
     

        // Delete monthly rent payment (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _monthlyRentPaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        // Delete monthly rent payment (POST)
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _monthlyRentPaymentRepository.DeleteAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Monthly rent payment deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete monthly rent payment.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {

            // If you need dropdown data (like leases, employees, vendors), load them here
            ViewBag.Leases = await _monthlyRentPaymentRepository.GetLeaseNameAsync();
            ViewBag.Employees = await _monthlyRentPaymentRepository.GetEmployeeNamesAsync();
            ViewBag.Vendors = await _monthlyRentPaymentRepository.GetOwnersAsync();
            ViewBag.TDSApplicable = await _monthlyRentPaymentRepository.GetTdsApplicableAsync();
        }
    }

}


