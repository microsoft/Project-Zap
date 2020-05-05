using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Project.Zap.Controllers;
using Project.Zap.Library.Models;
using Project.Zap.Models;
using Project.Zap.Services;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Project.Zap.Tests
{
    public class LocationsControllerTests
    {
        [Fact]
        public void Controller_Auth_ManagerPolicy()
        {
            AuthorizeAttribute attribute = typeof(LocationsController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task Index_NoParams_RepoGetHitWithNoParams()
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            LocationsController controller = GetController(locationService: service);

            // Act
            await controller.Index();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            service.Received(1).Get();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Index_NoParams_ResultModelIsEnumerableOfLocationViewModel()
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            LocationsController controller = GetController(locationService: service);

            // Act
            IActionResult result = await controller.Index();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<IEnumerable<LocationViewModel>>(viewResult.ViewData.Model);
        }

        [Fact]
        public void Add_NoParams_ReturnsViewResult()
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            LocationsController controller = GetController(locationService: service);

            // Act
            IActionResult result = controller.Add();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AddLocation_LocationViewModelInValid_ReturnsBadRequest()            
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            LocationsController controller = GetController(locationService: service);
            controller.ModelState.AddModelError("Name", "Required");
            AddLocationViewModel viewModel = new AddLocationViewModel();

            // Act
            IActionResult result = await controller.AddLocation(viewModel);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddLocation_LocationViewModel_AddOnServiceHitWithMappedLocation()
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            LocationsController controller = GetController(locationService: service);
            AddLocationViewModel viewModel = new AddLocationViewModel { Name = "Contoso", Address =  "Seattle", ZipOrPostcode = "54321" };

            // Act
            IActionResult result = await controller.AddLocation(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            service.Received(1).Add(Arg.Any<Location>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Delete_LocationName_RepoDeleteHitWithExpression()
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            LocationsController controller = GetController(locationService: service);
            string id = "Contoso";

            // Act
            await controller.Delete(id);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            service.Received(1).DeleteByName("Contoso");
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed            
        }

        [Fact]
        public async Task Edit_LocationName_ServiceGetsHitWithRightName()
        {
            // Arrange
            ILocationService service = Substitute.For<ILocationService>();
            service.GetByName(Arg.Any<string>()).Returns(new Location { id = "1", Name = "Contoso", Address = new Address { Text = "abc", ZipOrPostcode = "54321" } });
            LocationsController controller = GetController(locationService:service);
            string id = "Contoso";

            // Act
            await controller.Edit(id);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            service.Received(1).GetByName(id);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
        }

        private LocationsController GetController(
            ILocationService locationService = null,
             IConfiguration configuration = null
            ) 
        {
            locationService = locationService ?? Substitute.For<ILocationService>();
            configuration = configuration ?? Substitute.For<IConfiguration>();

            var controller = new LocationsController(locationService, configuration);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            return controller;
        }
    }
}
