using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class FileUploadViewModel
    {
        [BindProperty]
        [Display(Name = "StoreName")]
        public string StoreName { get; set; }

        [Display(Name = "StoreNames")]
        public SelectList StoreNames { get; set; }

        [BindProperty]
        [Display(Name = "FormFile")]
        public IFormFile FormFile { get; set; }
    }

}
