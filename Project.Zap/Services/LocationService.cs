using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Zap.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<Location>> Get();
        Task Add(Location location);

        Task<Location> GetByName(string name);
        Task DeleteByName(string name);
        Task Update(Location location);
        Task<IEnumerable<Location>> GetByDistance(Point point, int distanceInMeters);
    }
    public class LocationService : ILocationService
    {
        private readonly IRepository<Location> repository;
        private readonly IRepository<Shift> shiftRepository;
        private readonly IMapService mapService;

        public LocationService(IRepository<Location> repository, IRepository<Shift> shiftRepository, IMapService mapService)
        {
            this.repository = repository;
            this.shiftRepository = shiftRepository;
            this.mapService = mapService;
        }

        public async Task Add(Location location)
        {
            location.Address.Point = await this.mapService.GetCoordinates(location.Address);

            await this.repository.Add(location);
        }

        public async Task DeleteByName(string name)
        {
            Location existing = await this.GetByName(name);
            await this.repository.Delete(x => x.Name == name);
            await this.shiftRepository.Delete(x => x.LocationId == existing.id);
        }

        public async Task Update(Location location)
        {
            Location existing = await this.GetByName(location.Name);
            existing.Address = location.Address;
            existing.Address.Point = await this.mapService.GetCoordinates(location.Address);
            await this.repository.Replace(existing);
        }

        public async Task<IEnumerable<Location>> Get()
        {
            IEnumerable<Location> locations = await this.repository.Get();
            
            foreach(Location location in locations)
            {
                if(location.Address.Point == null)
                {
                    location.Address.Point = await this.mapService.GetCoordinates(location.Address);
                }
            }

            return locations;
        }

        public async Task<Location> GetByName(string name)
        {
            Location location =  (await this.repository.Get("SELECT * FROM c WHERE c.Name = @name", new Dictionary<string, object> { { "@name", name } })).FirstOrDefault();
            
            if (location.Address.Point == null)
            {
                location.Address.Point = await this.mapService.GetCoordinates(location.Address);
            }

            return location;
        }

        public async Task<IEnumerable<Location>> GetByDistance(Point point, int distanceInMeters)
        {
           return await this.repository.Get(
                    $"SELECT * FROM c WHERE ST_DISTANCE(c.Address.Point, {{'type': 'Point', 'coordinates':[{point.coordinates[0]}, {point.coordinates[1]}]}}) < @radiusDistance",
                    new Dictionary<string, object>
                    {
                        {"@radiusDistance", distanceInMeters }
                    });
        }
    }
}
