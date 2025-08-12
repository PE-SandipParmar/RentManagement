using Microsoft.AspNetCore.Mvc;

namespace RentManagement.Controllers
{
    public class Dashboard : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
