using Project.Zap.Library.Models;
using System.Collections.Generic;
using System.Net.Http;
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

        public Task<IEnumerable<Address>> GetAddresses(string zipOrPostcode)
        {
            throw new System.NotImplementedException();
        }

        public Task<Coordinates> GetCoordinates(Address address)
        {
            throw new System.NotImplementedException();
        }
    }
}
