using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Project.Zap.Library.Models
{
    public class Shift
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();
        
        public string LocationId { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string WorkType { get; set; }

        public bool Allocated { get; set; }

        public string EmployeeId { get; set; }
    }

}
