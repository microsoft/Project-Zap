using Project.Zap.Library.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class AzureMapsService : IMapService
    {
        private readonly HttpClient httpClient;
        private readonly string azureMapsKey;

        public AzureMapsService(HttpClient httpClient, string azureMapsKey)
        {
            this.httpClient = httpClient;
            this.azureMapsKey = azureMapsKey;
        }

        public async Task<Point> GetCoordinates(Address address)
        {
            string json = await this.httpClient.GetStringAsync($"search/address/json?api-version=1.0&subscription-key={this.azureMapsKey}&query={address.Text},{address.ZipOrPostcode}");
            MapsResponse response = JsonSerializer.Deserialize<MapsResponse>(json);

            return new Point
            {
                coordinates = new double[]
                {
                    response.results[0].position.lat,
                    response.results[0].position.lon
                }
            };
        }
    }

    public class MapsResponse
    {
        public List<MapsResponseResult> results { get; set; } 
    }

    public class MapsResponseResult
    {
        public MapsResponseResultPosition position { get; set; }
    }

    public class MapsResponseResultPosition
    {
        public double lat { get; set; }

        public double lon { get; set; }
    }
}
