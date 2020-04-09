using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Project.Zap.Library.Models
{
    public class Organization
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }

        public StoreTypes StoreType { get; set; }

        public List<string> ManagerEmails { get; set; }

        public List<Store> Stores { get; set; }
    }
}
