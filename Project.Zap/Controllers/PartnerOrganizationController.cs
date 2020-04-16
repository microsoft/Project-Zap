using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "OrgAManager")]
    public class PartnerOrganizationController : Controller
    {
        private readonly Random random;
        private readonly IRepository<PartnerOrganization> repository;

        public PartnerOrganizationController(IRepository<PartnerOrganization> repository)
        {
            this.random = new Random();
            this.repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<PartnerOrganization> partners = await this.repository.Get();
            return View("Index", partners.Map());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }
       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPartner(PartnerOrganizationViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            PartnerOrganization partner = viewModel.Map();
            partner.RegistrationCode = this.GetCode();

            await this.repository.Add(partner);

            return await this.Index();   
        }

        [HttpGet]
        public async Task<IActionResult> DeletePartner(string id)
        {
            await this.repository.Delete(x => x.Name == id);
            
            return await this.Index();
        }

        private char[] chars = new char[]
        {
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
            '0','1','2','3','4','5','6','7','8','9',
            '!','£','@','#','<','>'
        };

        private string GetCode()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                int next = this.random.Next(0, this.chars.Length);
                builder.Append(this.chars[next]);
            }

            return builder.ToString();            
        }
    }
}
