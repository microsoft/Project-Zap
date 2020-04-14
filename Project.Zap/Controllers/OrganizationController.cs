using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "OrgAManager")]
    public class OrganizationController : Controller
    {
        private readonly IRepository<Organization> organizationRepository;
        private Organization organization;

        public OrganizationController(IRepository<Organization> organizationRepository)
        {
            this.organizationRepository = organizationRepository;
            this.organization = this.organizationRepository.Get().Result.FirstOrDefault();
        }

        public IActionResult Index()
        {           
            if(this.organization == null)
            {
                return View("Setup");
            }
            return View("Index", this.organization.Map());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStore(StoreViewModel viewModel)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Store store = viewModel.Map();

            if (this.organization.Stores == null) this.organization.Stores = new List<Store>();

            this.organization.Stores.Add(store);
            await this.organizationRepository.Update(this.organization);

            return View("Index", this.organization.Map());
        }

        [HttpGet]
        public async Task<IActionResult> DeleteStore(string id)
        {
            Store store = this.organization.Stores.Where(x => x.Name == id).FirstOrDefault();
            if (store == null) return View("Index", this.organization.Map());

            this.organization.Stores.Remove(store);
            await this.organizationRepository.Update(this.organization);

            return View("Index", this.organization.Map());
        }

        [HttpGet]
        public IActionResult EditStore(string id)
        {
            Store store = this.organization.Stores.Where(x => x.Name == id).FirstOrDefault();
            return View(store.Map());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStore(StoreViewModel viewModel)
        {
            Store store = viewModel.Map();

            Store oldStore = this.organization.Stores.Where(x => x.Name == store.Name).FirstOrDefault();
            if (oldStore == null) return View("Index", this.organization.Map());

            this.organization.Stores.Remove(oldStore);
            this.organization.Stores.Add(store);

            await this.organizationRepository.Update(this.organization);

            return View("Index", this.organization.Map());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(OrganizationViewModel viewModel)
        {
            if(this.organization != null)
            {
                return new NotFoundResult();
            }

            Claim email = HttpContext.User.Claims.Where(x => x.Type == "emails").FirstOrDefault();
            if (email == null)
            {
                throw new ArgumentException("Email claim must be present");
            }

            var organization = new Organization
            {
                Name = viewModel.Name,
                StoreType = Library.Models.StoreTypes.Open,
                ManagerEmails = new List<string>
                {
                    email.Value
                }
            };

            await this.organizationRepository.Add(organization);

            return Redirect("/Home");
        }
    }
}
