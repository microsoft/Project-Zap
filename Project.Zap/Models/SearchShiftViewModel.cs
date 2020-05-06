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
        public DateTime Start { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);

        [BindProperty]
        [Display(Name = "Location")]
        public List<string> Locations { get; set; }

        [Display(Name = "ZipOrPostcode")]
        [BindProperty]
        public string ZipOrPostcode { get; set; }

        [Display(Name = "Distance")]
        [BindProperty]
        public int? DistanceInMeters { get; set; }

        public SelectList Distances { get; set; } = new SelectList(new[] { new SelectListItem { Text = "10 Miles", Value = "16093" }, new SelectListItem { Text = "30 Miles", Value = "48280" }}, "Value", "Text");

        [BindProperty]
        public string FilterByLocation { get; set; }

        public IEnumerable<ShiftViewModel> Result { get; set; }

        public IEnumerable<MapPointViewModel> MapPoints { get; set; }

        [BindProperty]
        [Display(Name = "Available")]
        public bool Available { get; set; } = true;

        [BindProperty]
        [Display(Name = "UseMyLocation")]
        public bool UseMyLocation { get; set; } = true;

    }
}
