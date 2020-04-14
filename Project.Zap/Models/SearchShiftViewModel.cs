using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class SearchShiftViewModel
    {
        [BindProperty]
        [Display(Name = "StoreNames")]
        public SelectList StoreNames { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "Start")]
        public DateTime Start { get; set; } = DateTime.Now;

        public ShiftViewModel NewShift { get; set; }

        public IEnumerable<ShiftViewModel> Result { get; set; }

        [Display(Name = "Available")]
        public bool Available { get; set; } = true;
        
    }
}
