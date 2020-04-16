using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using NSubstitute;
using Project.Zap.Controllers;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Project.Zap.Tests
{
    public class ShiftsControllerTests
    {
        [Fact]
        public void Controller_Auth_ShiftViewerPolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetCustomAttribute<AuthorizeAttribute>();
            attribute.Policy = "ShiftViewer";
        }

        [Fact]
        public async Task Index_NoParams_RedirectIfNoLocations()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new Library.Models.Location[] { });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);

            // Act
            IActionResult result = await controller.Index();

            // Assert
            RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/Locations", redirectResult.Url);
        }

        [Fact]
        public async Task Index_NoParams_ReturnsShiftsForLaterThanNow()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();            
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);

            // Act
            await controller.Index();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get("SELECT * FROM c WHERE c.Start > @start", Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("@start")));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Search_SearchViewModel_LocationsRepoHitOnce()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1" },
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "2" },
                new Library.Models.Shift { Start = DateTime.Now, LocationId = "1" },
            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            SearchShiftViewModel viewModel = new SearchShiftViewModel { Location = "Contoso", Start = DateTime.Now };

            // Act
            await controller.Search(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            locationRepository.Received(1).Get();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Search_SearchViewModel_ResultsFilteredByLocation()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new []
            {
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1" },
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "2" },
                new Library.Models.Shift { Start = DateTime.Now, LocationId = "1" },
            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            SearchShiftViewModel viewModel = new SearchShiftViewModel { Location = "Contoso", Start = DateTime.Now };

            // Act
            IActionResult result = await controller.Search(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            SearchShiftViewModel viewModelResult = Assert.IsAssignableFrom<SearchShiftViewModel>(viewResult.ViewData.Model);
            Assert.Contains<ShiftViewModel>(viewModelResult.Result, x => x.LocationName == "Contoso");
            Assert.DoesNotContain<ShiftViewModel>(viewModelResult.Result, x => x.LocationName == "Fabrikam");
        }

        [Fact]
        public async Task Search_SearchViewModel_ResultsFilteredByAvailable()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true },
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = false }
                
            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            SearchShiftViewModel viewModel = new SearchShiftViewModel { Location = "Contoso", Start = DateTime.Now.Add(new TimeSpan(1, 0, 0, 0)) };

            // Act
            IActionResult result = await controller.Search(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            SearchShiftViewModel viewModelResult = Assert.IsAssignableFrom<SearchShiftViewModel>(viewResult.ViewData.Model);
            Assert.Single(viewModelResult.Result);
        }

        [Fact]
        public async Task Search_SearchViewModel_ResultsFilteredByDate()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { Start = DateTime.Now, LocationId = "1", Allocated = false },
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = false }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            SearchShiftViewModel viewModel = new SearchShiftViewModel { Location = "Contoso", Start = DateTime.Now };

            // Act
            IActionResult result = await controller.Search(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            SearchShiftViewModel viewModelResult = Assert.IsAssignableFrom<SearchShiftViewModel>(viewResult.ViewData.Model);
            Assert.Single(viewModelResult.Result);
        }

        [Fact]
        public void Delete_Attributes_HasManagerPolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("Delete").GetCustomAttribute<AuthorizeAttribute>();
            attribute.Policy = "OrgAManager";
        }

        [Fact]
        public async Task Delete_ShiftViewModel_QueryPassedToRepo()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);

            DateTime now = DateTime.Now;
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            await controller.Delete(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Delete(Arg.Any<Expression<Func<Library.Models.Shift, bool>>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public void ViewShifts_Attributes_HasEmployeePolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("ViewShifts").GetCustomAttribute<AuthorizeAttribute>();
            attribute.Policy = "OrgBEmployee";
        }

        [Fact]
        public async Task ViewShifts_NoIdClaim_Exception()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);

            // Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.ViewShifts());
            Assert.Equal("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required", exception.Message);
        }

        [Fact]
        public async Task ViewShifts_NoShifts_ViewDataWarning()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();            
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            IActionResult result = await controller.ViewShifts();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("You have no shifts booked.", viewResult.ViewData["NoShifts"]);
        }

        [Fact]
        public async Task ViewShifts_Shifts_ViewModel()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(new[]
            {
                new Library.Models.Shift { Start = DateTime.Now, LocationId = "1", Allocated = false },
                new Library.Models.Shift { Start = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = false }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            IActionResult result = await controller.ViewShifts();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            IEnumerable<ShiftViewModel> viewModelResult = Assert.IsAssignableFrom<IEnumerable<ShiftViewModel>>(viewResult.ViewData.Model);
            Assert.Equal(2, viewModelResult.Count());
        }

        [Fact]
        public async Task ViewShifts_Shifts_AfterToday()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            await controller.ViewShifts();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get("SELECT * FROM c WHERE c.EmployeeId = @employeeId AND c.Start > @start", Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("@employeeId") && x.ContainsKey("@start")));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public void ViewShift_Attributes_HasManagerPolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("ViewShift").GetCustomAttribute<AuthorizeAttribute>();
            attribute.Policy = "OrgAManager";
        }

    }
}
