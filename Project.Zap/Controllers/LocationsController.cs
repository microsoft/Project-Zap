using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Models;
using Project.Zap.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "OrgAManager")]
    public class LocationsController : Controller
    {
        private readonly ILocationService locationService;
        private readonly IConfiguration configuration;

        public LocationsController(ILocationService locationService, IConfiguration configuration)
        {
            this.locationService = locationService;
            this.configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Location> locations = await this.locationService.Get();
            return View("Index", locations.Map());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(AddLocationViewModel viewModel)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await this.locationService.Add(viewModel.Map());

            return Redirect("/Locations");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            await this.locationService.DeleteByName(id);
            return Redirect("/Locations");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            Location location = await this.locationService.GetByName(id);
            ViewData["AzureMapsKey"] = this.configuration["AzureMapsSubscriptionKey"];
            return View(location.Map());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel viewModel)
        {
            await this.locationService.Update(viewModel.Map());

            return Redirect("/Locations");
        }
    }
}
