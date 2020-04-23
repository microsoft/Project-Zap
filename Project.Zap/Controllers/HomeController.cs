using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Zap.Models;

namespace Project.Zap.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            Claim zapRole = HttpContext.User.Claims.Where(x => x.Type == "extension_zaprole").FirstOrDefault();

            if (zapRole == null)
            {
                return View();
            }

            if(zapRole.Value == "org_a_manager")
            {
                return Redirect("/Shifts");
            }

            if(zapRole.Value == "org_b_employee")
            {
                return Redirect("/Shifts/ViewShifts");
            }

            return View();
        }


        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
