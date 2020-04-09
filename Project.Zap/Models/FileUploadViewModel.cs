using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Project.Zap.Models
{
    public class FileUploadViewModel
    {
        [BindProperty]
        public string StoreName { get; set; }

        public SelectList StoreNames { get; set; }

        [BindProperty]
        public IFormFile FormFile { get; set; }
    }

}
