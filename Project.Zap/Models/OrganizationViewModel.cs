using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Project.Zap.Models
{
    public class OrganizationViewModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();

        [BindProperty]
        [Required, StringLength(30)]
        public string Name { get; set; }

        public StoreTypes StoreType { get; set; }

        public List<StoreViewModel> Stores { get; set; }
    }
}
