using Project.Zap.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public class AzureMapsService : IMapService
    {
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
