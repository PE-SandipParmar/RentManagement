using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Threading.Tasks;

namespace RentManagement.Controllers
{

    public class MonthlyRentPaymentController : Controller
    {
        private readonly ILeaseRepository _leaseRepository;
        private readonly IMonthlyRentPaymentRepository _monthlyRentPaymentRepository;

        public MonthlyRentPaymentController(
            ILeaseRepository leaseRepository,
            IMonthlyRentPaymentRepository monthlyRentPaymentRepository)
        {
            _leaseRepository = leaseRepository;
            _monthlyRentPaymentRepository = monthlyRentPaymentRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var MonthlyRentPayment = await _monthlyRentPaymentRepository.GetAllAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            return View(MonthlyRentPayment);
        }
        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            _monthlyRentPaymentRepository.ToggleActiveStatus(id);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _monthlyRentPaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorsByEmployee(int employeeId)
        {
            if (employeeId <= 0)
                return Json(new { success = false, message = "Invalid employee ID" });

            var vendors = await _monthlyRentPaymentRepository.GetOwnersByEmployeeAsync(employeeId);
            return Json(new { success = true, vendors = vendors });
        }

        [HttpGet]
        public async Task<IActionResult> GetLeasesByEmployeeAndVendor(int employeeId, int vendorId)
        {
            if (employeeId == null || vendorId <= 0)
                return Json(new { success = false, message = "Invalid employee or vendor ID" });

            var leases = await _monthlyRentPaymentRepository.GetLeasesByEmployeeAndVendorAsync(employeeId, vendorId);
            return Json(new { success = true, leases = leases });
        }


        [HttpGet]
        public async Task<IActionResult> GetLeasesDetails(int id)
        {
            var lease = await _leaseRepository.GetLeaseByIdAsync(id);
            return Json(new { success = true, lease = lease });
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

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

        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _monthlyRentPaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();
            await LoadDropdowns();

            return View(payment);
        }

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
     

        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _monthlyRentPaymentRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

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

            //ViewBag.Leases = await _monthlyRentPaymentRepository.GetLeaseNameAsync();
            ViewBag.Employees = await _monthlyRentPaymentRepository.GetEmployeeNamesAsync();
            //ViewBag.Vendors = await _monthlyRentPaymentRepository.GetOwnersByEmployeeAsync(employeename);
            ViewBag.TDSApplicable = await _monthlyRentPaymentRepository.GetTdsApplicableAsync();
        }
    }

}


