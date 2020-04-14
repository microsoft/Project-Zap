using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class AddressViewModel
    {
        [BindProperty]
        [Required, StringLength(30)]
        [Display(Name = "City")]
        public string City { get; set; }

        [BindProperty]
        [Required, StringLength(8)]
        [Display(Name = "ZipOrPostCode")]
        public string ZipOrPostCode { get; set; }
    }

}
