using Dapper;
using Microsoft.AspNetCore.Mvc;

using RentManagement.Data;
using RentManagement.Models;
using RentManagement.Models.RentPaymentSystem.Models;
using System.Data.SqlClient;
using System.Data;
using System.Text.Json;
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
