using System;
using System.Text.Json.Serialization;

namespace Project.Zap.Library.Models
{
    public class Shift
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();
        
        public string LocationId { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string WorkType { get; set; }

        public bool Allocated { get; set; }

        public string EmployeeId { get; set; }
    }

}
