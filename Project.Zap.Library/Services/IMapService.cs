using Project.Zap.Library.Models;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public interface IMapService
    {
        Task<Point> GetCoordinates(Address address);
    }
}
