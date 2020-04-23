using System.Text.Json.Serialization;

namespace Project.Zap.Models
{
    public class PointViewModel
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Point";

        [JsonPropertyName("coordinates")]
        public double[] Coordinates { get; set; }
    }
}
