using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class AddressViewModel
    {
        [BindProperty]
        [Required, StringLength(30)]
        public string City { get; set; }

        [BindProperty]
        [Required, StringLength(8)]
        public string ZipOrPostCode { get; set; }
    }

}
