using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class FileUploadViewModel
    {
        [BindProperty]
        [Display(Name = "LocationName")]
        public string LocationName { get; set; }

        [Display(Name = "LocationNames")]
        public SelectList LocationNames { get; set; }

        [BindProperty]
        [Display(Name = "FormFile")]
        public IFormFile FormFile { get; set; }
    }

}
