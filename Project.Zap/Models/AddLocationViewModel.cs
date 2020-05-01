using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class AddLocationViewModel
    {

        [BindProperty]
        [Required, StringLength(30, MinimumLength = 5)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "ZipOrPostcode")]
        public string ZipOrPostcode { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

    }

    [BindRequired]
    public class LocationViewModel
    {

        [BindProperty]
        [Required, StringLength(30, MinimumLength = 5)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "ZipOrPostcode")]
        public string ZipOrPostcode { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }
    }
}
