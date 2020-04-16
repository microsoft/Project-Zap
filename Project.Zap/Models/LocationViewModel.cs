using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Project.Zap.Models
{
    [BindRequired]
    public class LocationViewModel
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
