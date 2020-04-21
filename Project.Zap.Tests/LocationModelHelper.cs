using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Project.Zap.Tests
{
    public class LocationModelHelper
    {
        [Fact]
        public void Map_EnumerableOfLocation_EnumerableOfLocationViewModel()
        {
            // Arrange
            IEnumerable<Location> locations = new List<Location>
            {
                new Location { Name = "Contoso", Address = new Address { Text = "Seattle", ZipOrPostcode = "54321" } },
                new Location { Name = "Fabrikam", Address = new Address { Text = "London", ZipOrPostcode = "12345" } }
            };

            // Act
            IList<LocationViewModel> viewModels = locations.Map().ToList();

            // Assert
            Assert.Equal("Contoso", viewModels[0].Name);
            Assert.Equal("Fabrikam", viewModels[1].Name);
            Assert.Equal("Seattle", viewModels[0].Address);
            Assert.Equal("London", viewModels[1].Address);
            Assert.Equal("54321", viewModels[0].ZipOrPostcode);
            Assert.Equal("12345", viewModels[1].ZipOrPostcode);
        }

        [Fact]
        public void Map_Location_LocationViewModel()
        {
            // Arrange
            Location location = new Location { Name = "Contoso", Address = new Address { Text = "Seattle", ZipOrPostcode = "54321" } };
            
            // Act
            LocationViewModel viewModel = location.Map();

            // Assert
            Assert.Equal("Contoso", viewModel.Name);
            Assert.Equal("Seattle", viewModel.Address);
            Assert.Equal("54321", viewModel.ZipOrPostcode);
        }

        [Fact]
        public void Map_LocationViewModel_Location()
        {
            // Arrange
            AddLocationViewModel viewModel = new AddLocationViewModel { Name = "Contoso", Address =  "Seattle", ZipOrPostcode = "54321"  };

            // Act
            Location location = viewModel.Map();

            // Assert
            Assert.Equal("Contoso", location.Name);
            Assert.Equal("Seattle", location.Address.Text);
            Assert.Equal("54321", location.Address.ZipOrPostcode);
        }
    }
}
