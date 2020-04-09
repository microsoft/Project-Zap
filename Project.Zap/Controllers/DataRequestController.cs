using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.Zap.Controllers
{
    [Authorize]
    public class DataRequestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
