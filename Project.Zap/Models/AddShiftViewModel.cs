using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class AddShiftViewModel
    {
        [Display(Name = "StoreNames")]
        public SelectList LocationNames { get; set; }

        [BindProperty]
        public ShiftViewModel NewShift { get; set; }
    }
}
