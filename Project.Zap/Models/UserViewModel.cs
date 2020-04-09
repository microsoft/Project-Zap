using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class UserViewModel
    {
        [BindProperty]
        [Required, StringLength(30)]
        public string DisplayName { get; set; }

        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; }
    }

}
