using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class PartnerOrganizationViewModel
    {
        [BindProperty]
        [Required, StringLength(30, MinimumLength = 5)]
        public string Name { get; set; }

        [BindProperty]        
        public string RegistrationCode { get; set; }
    }
}
