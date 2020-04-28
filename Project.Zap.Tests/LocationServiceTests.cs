using Project.Zap.Library.Services;
using Project.Zap.Library.Models;
using Project.Zap.Services;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Project.Zap.Tests
{
    public class LocationServiceTests
    {
        [Fact]
        public async Task Add_Location_GetsCoordinates()
        {
            // Arrange
            IMapService mapService = Substitute.For<IMapService>();
            ILocationService service = this.GetLocationService(mapService: mapService);
            Location location = new Location { Address = new Address() };

            // Act           
            await service.Add(location);

            //Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            mapService.Received(1).GetCoordinates(location.Address);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Add_Location_HitsRepository()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            ILocationService service = this.GetLocationService(locationRepository: repository);
            Location location = new Location { Address = new Address() };

            // Act           
            await service.Add(location);

            //Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Add(location);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task DeleteById_Location_HitsLocationRepository()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            Location location = new Location { Address = new Address { } };
            repository.Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Returns(new[] { location });
            ILocationService service = this.GetLocationService(locationRepository: repository);            

            // Act           
            await service.DeleteByName("Contoso");

            //Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Delete(Arg.Any<Expression<Func<Location, bool>>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task DeleteById_Location_HitsShiftRepository()
        {
            // Arrange
            IRepository<Shift> shiftRepository = Substitute.For<IRepository<Shift>>();
            IRepository<Location> locationRepository = Substitute.For<IRepository<Location>>();
            Location location = new Location { Address = new Address { } };
            locationRepository.Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Returns(new[] { location });
            ILocationService service = this.GetLocationService(shiftRepository: shiftRepository, locationRepository: locationRepository);

            // Act           
            await service.DeleteByName("Contoso");

            //Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Delete(Arg.Any<Expression<Func<Shift, bool>>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task GetByDistance_PointAndDistance_RepoHitWithQuery()
        {
            // Arrange
            IRepository<Location> locationRepository = Substitute.For<IRepository<Location>>();
            Location location = new Location { Address = new Address { } };
            locationRepository.Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Returns(new[] { location });
            ILocationService service = this.GetLocationService(locationRepository: locationRepository);

            // Act           
            await service.GetByDistance(new Point { coordinates = new double[] { 1, 2 } }, 1000);

            //Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            locationRepository.Received(1).Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }


        [Fact]
        public async Task Update_Location_GetAndReplaceCalledOnRepo()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            IMapService mapService = Substitute.For<IMapService>();
            Location location = new Location { Address = new Address { Point = new Point() } };
            repository.Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Returns(new[] { location });
            ILocationService service = this.GetLocationService(locationRepository: repository, mapService: mapService);
            

            // Act
            await service.Update(location);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Get("SELECT * FROM c WHERE c.Name = @name", Arg.Any<Dictionary<string, object>>());
            repository.Received(1).Replace(location);
            mapService.Received(1).GetCoordinates(location.Address);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
        }

        [Fact]
        public async Task GetByName_Location_GetOnRepo()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            Location location = new Location { Address = new Address { } };
            repository.Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Returns(new[] { location });
            ILocationService service = this.GetLocationService(locationRepository: repository);

            // Act
            await service.GetByName("Contoso");

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Get("SELECT * FROM c WHERE c.Name = @name", Arg.Any<Dictionary<string, object>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
        }

        [Fact]
        public async Task Get_LocationWithNullPoint_CallMapService()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            repository.Get().Returns(new[] { new Location { Address = new Address { } } });
            IMapService mapService = Substitute.For<IMapService>();
            ILocationService service = this.GetLocationService(locationRepository: repository, mapService: mapService);

            // Act
            await service.Get();

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            mapService.Received(1).GetCoordinates(Arg.Any<Address>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
        }

        [Fact]
        public async Task GetByName_LocationWithNullPoint_CallMapService()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            repository.Get(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Returns(new[] { new Location { Address = new Address { } } });
            IMapService mapService = Substitute.For<IMapService>();
            ILocationService service = this.GetLocationService(locationRepository: repository, mapService: mapService);

            // Act
            await service.GetByName("Contoso");

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            mapService.Received(1).GetCoordinates(Arg.Any<Address>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
        }

        private ILocationService GetLocationService(
            IRepository<Location> locationRepository = null,
            IRepository<Shift> shiftRepository = null,
            IMapService mapService = null)
        {
            locationRepository = locationRepository ?? Substitute.For<IRepository<Location>>();
            shiftRepository = shiftRepository ?? Substitute.For<IRepository<Shift>>();
            mapService = mapService ?? Substitute.For<IMapService>();

            return new LocationService(locationRepository, shiftRepository, mapService);
        }
    }
}
