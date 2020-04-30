using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
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
    [Authorize]
    public class NewUserController : Controller
    {
        private readonly IGraphServiceClient graphClient;
        private readonly IRepository<PartnerOrganization> partnerRepository;
        private readonly IStringLocalizer<NewUserController> stringLocalizer;
        private readonly ILogger<NewUserController> logger;
        private readonly string managerCode;
        private readonly string extensionId;

        public NewUserController(
            IGraphServiceClient graphClient, 
            IRepository<PartnerOrganization> partnerRepository,
            IStringLocalizer<NewUserController> stringLocalizer,
            IConfiguration configuration,
            ILogger<NewUserController> logger)
        {
            this.graphClient = graphClient;
            this.partnerRepository = partnerRepository;
            this.stringLocalizer = stringLocalizer;
            this.logger = logger;
            this.managerCode = configuration["ManagerCode"];
            this.extensionId = configuration["ExtensionId"];
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(NewUserViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Claim id = this.HttpContext.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();
            if (id == null)
            {
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
            }

            if (managerCode == viewModel.RegistrationCode)
            {
                await this.UpdateUserRole("org_a_manager", id.Value);
            }
            else
            {

                PartnerOrganization partner = (await this.partnerRepository.Get(
                    "SELECT * FROM c WHERE c.RegistrationCode = @registrationCode",
                    new Dictionary<string, object> { { "@registrationCode", viewModel.RegistrationCode } })).FirstOrDefault();

                if (partner == null)
                {
                    ViewData["ErrorMessage"] = this.stringLocalizer["WrongCodeError"];
                    return View("Index");
                }

                await this.UpdateUserRole("org_b_employee", id.Value);                
            }

            return Redirect("/AzureADB2C/Account/SignOut");
        }

        private async Task UpdateUserRole(string role, string id, int retries = 0)
        {
            IDictionary<string, object> extensions = new Dictionary<string, object>();
            extensions.Add($"extension_{this.extensionId}_zaprole", role);

            var adUser = new User
            {
                AdditionalData = extensions
            };

            try
            {
                await this.graphClient.Users[id].Request().UpdateAsync(adUser);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception.Message);
                await Task.Delay(1000);
                retries++;
                if (retries > 2) throw exception;

                await this.UpdateUserRole(role, id, retries);
            }
        }
    }
    
}
