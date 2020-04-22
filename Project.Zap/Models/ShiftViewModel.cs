using Microsoft.AspNetCore.Mvc;
using Project.Zap.Filters;
using System;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Models
{

    public class ShiftViewModel
    {
        [BindProperty]
        [Required]
        [Display(Name = "StoreName")]
        public string LocationName { get; set; }
        
        [BindProperty]
        [Required]
        [Display(Name = "Start")]
        [DateLessThan("End")]
        [DisplayFormat(DataFormatString = "{yyyy-MM-ddTHH:mm}")]
        public DateTime Start { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

        [BindProperty]
        [Required]
        [Display(Name = "End")]
        [DisplayFormat(DataFormatString = "{yyyy-MM-ddTHH:mm}")]
        public DateTime End { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

        [BindProperty]
        [Required]
        [Display(Name = "WorkType")]
        public string WorkType { get; set; }

        [BindProperty]
        [Required]   
        [Range(1, 200)]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [BindProperty]
        [Required]
        [Range(0, 200)]
        [Display(Name = "Available")]
        public int Available { get; set; }
        public PointViewModel Point { get; internal set; }
    }
}
