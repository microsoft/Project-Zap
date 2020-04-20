using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "OrgAManager")]
    public class LocationsController : Controller
    {
        private readonly IRepository<Location> repository;

        public LocationsController(IRepository<Location> repository)
        {
            this.repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Location> locations = await this.repository.Get();
            return View("Index", locations.Map());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel viewModel)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Location location = viewModel.Map();

            await this.repository.Add(location);
            return await this.Index();
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            await this.repository.Delete(x => x.Name == id);
            return await this.Index();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            Location location = (await this.repository.Get("SELECT * FROM c WHERE c.Name = @name", new Dictionary<string, object> { {"@name", id } })).FirstOrDefault();
            return View(location.Map());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel viewModel)
        {
            Location location = viewModel.Map();

            await this.repository.Update(location);

            return await this.Index();
        }
    }
}
