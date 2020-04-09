using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class SearchShiftViewModel
    {
        public SelectList StoreNames { get; set; }

        public ShiftViewModel NewShift { get; set; }

        public IEnumerable<ShiftViewModel> Result { get; set; }
    }

    public class ShiftViewModel
    {
        [BindProperty]
        [Required]
        public string StoreName { get; set; }
        
        [BindProperty]
        [Required]
        [DisplayFormat(DataFormatString = "{yyyy-MM-ddTHH:mm}")]
        public DateTime Start { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

        [BindProperty]
        [Required]
        [DisplayFormat(DataFormatString = "{yyyy-MM-ddTHH:mm}")]
        public DateTime End { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

        [BindProperty]
        [Required]
        public string WorkType { get; set; }

        [BindProperty]
        [Required]        
        public int Quantity { get; set; }

        [BindProperty]
        [Required]
        public int Available { get; set; }

    }
}
