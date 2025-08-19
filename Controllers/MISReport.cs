using Microsoft.AspNetCore.Mvc;
using RentManagement.Data;
using RentManagement.Models;
using System.Threading.Tasks;

namespace RentManagement.Controllers
{
    public class MISReportController : Controller
    {
        

        public async Task<IActionResult> Index()
        {
          
            return View();
        }

    }
}
