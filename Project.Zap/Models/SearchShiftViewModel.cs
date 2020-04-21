using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{
    public class SearchShiftViewModel
    {
        [Display(Name = "StoreNames")]
        public SelectList LocationNames { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "Starting from")]
        [DisplayFormat(DataFormatString = "{yyyy-MM-ddTHH:mm}")]
        public DateTime Start { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

        [BindProperty]
        [Display(Name = "Location")]
        public List<string> Locations { get; set; }

        [BindProperty]
        public string ZipOrPostcode { get; set; }

        [BindProperty]
        public string DistanceInMeters { get; set; }

        public IEnumerable<ShiftViewModel> Result { get; set; }

        [Display(Name = "Available")]
        public bool Available { get; set; } = true;
        
    }
}
