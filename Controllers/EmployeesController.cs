using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;

namespace RentManagement.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            var employees = await _employeeRepository.GetEmployeesAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            return View(employees);
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (await _employeeRepository.EmailExistsAsync(employee.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }
            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
            {
                decimal monthlySalary = employee.TotalSalary.Value / 12;
                if (employee.HouseRentAllowance.Value > monthlySalary)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month’s salary.");
                }
            }
            if (ModelState.IsValid)
            {
                var employeeId = await _employeeRepository.CreateEmployeeAsync(employee);
                TempData["SuccessMessage"] = "Employee created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();

            // return the same View with the model and validation errors
            return View(employee);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id)
                return NotFound();


            if (await _employeeRepository.EmailExistsAsync(employee.Email,employee.Id))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
            }
            if (employee.TotalSalary.HasValue && employee.HouseRentAllowance.HasValue)
            {
                decimal monthlySalary = employee.TotalSalary.Value / 12;
                if (employee.HouseRentAllowance.Value > monthlySalary)
                {
                    ModelState.AddModelError("HouseRentAllowance", "House Rent Allowance cannot be more than one month’s salary.");
                }
            }

            if (ModelState.IsValid)
            {
                var success = await _employeeRepository.UpdateEmployeeAsync(employee);
                if (success)
                {
                    TempData["SuccessMessage"] = "Employee updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update employee.";
                }
            }

            ViewBag.Departments = await _employeeRepository.GetDepartmentsAsync();
            ViewBag.Designations = await _employeeRepository.GetDesignationsAsync();

            // return the same View with the model and validation errors
            return View(employee);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return View(employee);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _employeeRepository.DeleteEmployeeAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Employee deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete employee.";
            }

            return RedirectToAction(nameof(Index));
        }



    }
}