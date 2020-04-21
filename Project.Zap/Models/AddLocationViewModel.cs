using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public string ZipOrPostcode { get; set; }

        [BindProperty]
        public string Address { get; set; }

        public SelectList Addresses { get; set; }
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
        public string ZipOrPostcode { get; set; }

        public string Address { get; set; }
    }
}
