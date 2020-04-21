using Microsoft.Azure.Cosmos.Spatial;
using Project.Zap.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public interface IMapService
    {
        Task<IEnumerable<Address>> GetAddresses(string zipOrPostcode);

        Task<Coordinates> GetCoordinates(Address address);

    }
}
