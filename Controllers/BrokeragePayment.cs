using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrokeragePayment payment)
        {
            if (ModelState.IsValid)
            {
                var newId = await _BrokeragePaymentRepository.CreateAsync(payment);
                TempData["SuccessMessage"] = "Brokerage Payement created successfully!";
                return RedirectToAction(nameof(Index));
            }
            await LoadDropdowns();

            return View(payment);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _BrokeragePaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();
            await LoadDropdowns();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BrokeragePayment payment)
        {
            if (id != payment.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var success = await _BrokeragePaymentRepository.UpdateAsync(payment);
                if (success)
                {
                    TempData["SuccessMessage"] = "Brokerage updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update Brokerage Payment.";
                }
            }
            await LoadDropdowns();

            return View(payment);
        }
     

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

        private async Task LoadDropdowns()
        {

            ViewBag.Leases = await _BrokeragePaymentRepository.GetLeaseNameAsync();
            ViewBag.Employees = await _BrokeragePaymentRepository.GetEmployeeNamesAsync();
            ViewBag.Vendors = await _BrokeragePaymentRepository.GetOwnersAsync();
            ViewBag.TDSApplicable = await _BrokeragePaymentRepository.GetTdsApplicableAsync();
        }
    }

}


