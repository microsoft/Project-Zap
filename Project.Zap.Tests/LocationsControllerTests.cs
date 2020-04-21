using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Project.Zap.Controllers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);

            // Act
            await controller.Index();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Get();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Index_NoParams_ResultModelIsEnumerableOfLocationViewModel()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);

            // Act
            IActionResult result = await controller.Index();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<IEnumerable<AddLocationViewModel>>(viewResult.ViewData.Model);
        }

        [Fact]
        public void Add_NoParams_ReturnsViewResult()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);

            // Act
            IActionResult result = controller.Add();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AddLocation_LocationViewModelInValid_ReturnsBadRequest()            
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);
            controller.ModelState.AddModelError("Name", "Required");
            AddLocationViewModel viewModel = new AddLocationViewModel();

            // Act
            IActionResult result = await controller.AddLocation(viewModel);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddLocation_LocationViewModel_AddOnRepoHitWithMappedLocation()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);
            AddLocationViewModel viewModel = new AddLocationViewModel { Name = "Contoso", Address =  "Seattle", ZipOrPostcode = "54321" };

            // Act
            IActionResult result = await controller.AddLocation(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Add(Arg.Any<Location>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Delete_LocationName_RepoDeleteHitWithExpression()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);
            string id = "Contoso";

            // Act
            await controller.Delete(id);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Delete(Arg.Any<Expression<Func<Location, bool>>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed            
        }

        [Fact]
        public async Task Edit_LocationName_RepoGetHitWithCorrectQuery()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            repository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(new[] { new Location { Name = "Contoso", Address = new Address { Text = "Seattle", ZipOrPostcode = "54321" } } });
            LocationsController controller = new LocationsController(repository);
            string id = "Contoso";

            // Act
            IActionResult result = await controller.Edit(id);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Get("SELECT * FROM c WHERE c.Name = @name", Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("@name") && id == (string)x["@name"]));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task EditLocation_LocationViewModel_RepoGetHitWithExpression()
        {
            // Arrange
            IRepository<Location> repository = Substitute.For<IRepository<Location>>();
            LocationsController controller = new LocationsController(repository);
            AddLocationViewModel viewModel = new AddLocationViewModel { Name = "Contoso", Address =  "Seattle", ZipOrPostcode = "54321" };

            // Act
            IActionResult result = await controller.EditLocation(viewModel);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Update(Arg.Any<Location>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed  
            Assert.IsType<ViewResult>(result);
        }
    }
}
