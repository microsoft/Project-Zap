using System;
using System.Text.Json.Serialization;

namespace Project.Zap.Library.Models
{
    public class Employee
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string EmployeeId { get; set; }

        public string PartnerOrgId { get; set; }
    }

}
