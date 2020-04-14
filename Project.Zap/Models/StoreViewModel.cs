using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    [BindRequired]
    public class StoreViewModel
    {
        [BindProperty]
        [Required, StringLength(30, MinimumLength = 5)]
        [Display(Name = "Name")]
        public string Name { get; set; }        
        
        [BindProperty]
        [Required]
        public AddressViewModel Address { get; set; }
    }

}
