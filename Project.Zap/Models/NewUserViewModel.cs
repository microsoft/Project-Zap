using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class NewUserViewModel
    {
        [Required, StringLength(6, MinimumLength = 6)]
        [BindProperty]
        [Display(Name = "RegistrationCode")]
        public string RegistrationCode { get; set; }
    }
}
