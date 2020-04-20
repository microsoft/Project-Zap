using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using NSubstitute;
using Project.Zap.Controllers;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Text;
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
            Assert.Equal("ShiftViewer", attribute.Policy);
        }

        [Fact]
        public async Task Index_NoParams_RedirectIfNoLocations()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new Library.Models.Location[] { });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);

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
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            // Act
            await controller.Index();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get("SELECT * FROM c WHERE c.StartDateTime > @start", Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("@start")));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Search_SearchViewModel_LocationsRepoHitOnce()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1" },
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "2" },
                new Library.Models.Shift { StartDateTime = DateTime.Now, LocationId = "1" },
            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
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
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1" },
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "2" },
                new Library.Models.Shift { StartDateTime = DateTime.Now, LocationId = "1" },
            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
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
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true },
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = false }
                
            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
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
                new Library.Models.Shift { StartDateTime = DateTime.Now, LocationId = "1", Allocated = false },
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = false }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
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
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task Delete_ShiftViewModel_QueryPassedToRepo()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);

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
            Assert.Equal("OrgBEmployee", attribute.Policy);
        }

        [Fact]
        public async Task ViewShifts_NoIdClaim_Exception()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);

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
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
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
                new Library.Models.Shift { StartDateTime = DateTime.Now, LocationId = "1", Allocated = false },
                new Library.Models.Shift { StartDateTime = DateTime.Now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = false }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
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
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            await controller.ViewShifts();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get(
                "SELECT * FROM c WHERE c.EmployeeId = @employeeId AND c.StartDateTime > @start", 
                Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("@employeeId") && x.ContainsKey("@start")));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public void ViewShift_Attributes_HasManagerPolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("ViewShift").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task ViewShift_NoShifts_ViewDataWarning()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            IActionResult result = await controller.ViewShift(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("No employees are booked for this shift.", viewResult.ViewData["NoEmployees"]);
        }

        [Fact]
        public async Task ViewShift_EmployeesOnShift_GraphClientCalled()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            graphClient.Users[Arg.Any<string>()].Request().GetAsync().Returns(new User { GivenName = "a", Surname = "b" });
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            await controller.ViewShift(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            graphClient.Users.Received(1)[Arg.Is<string>(x => x == "abc")].Request().GetAsync();
            graphClient.Users.Received(1)[Arg.Is<string>(x => x == "xyz")].Request().GetAsync();            
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task ViewShift_EmployeesOnShift_EmployeesInViewData()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            graphClient.Users[Arg.Any<string>()].Request().GetAsync().Returns(new User { GivenName = "a", Surname = "b" });
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            IActionResult result = await controller.ViewShift(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            List<string> employees = Assert.IsType<List<string>>(viewResult.ViewData["Employees"]);
            Assert.Equal(2, employees.Count());
        }

        [Fact]
        public async Task ViewShift_EmployeesOnShift_ShiftRepoHitWithCorrectQuery()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            graphClient.Users[Arg.Any<string>()].Request().GetAsync().Returns(new User { GivenName = "a", Surname = "b" });
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            await controller.ViewShift(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get(
                "SELECT * FROM c WHERE c.LocationId = @locationId AND c.StartDateTime = @start AND c.EndDateTime = @end AND c.WorkType = @workType", 
                Arg.Any<Dictionary<string, object>>(),
                Arg.Any<string>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public void CancelShift_Attributes_HasEmployeePolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("CancelShift").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgBEmployee", attribute.Policy);
        }

        [Fact]
        public async Task CancelShift_NoIdClaim_Exception()
        {
            // Arrange
            DateTime now = DateTime.Now;
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.CancelShift(viewModel));
            Assert.Equal("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required", exception.Message);
        }

        [Fact]
        public async Task CancelShift_Shift_UpdateOnRepoHit()
        {
            // Arrange
            DateTime now = DateTime.Now;
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });

            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            IActionResult result = await controller.CancelShift(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Update(Arg.Is<Library.Models.Shift>(x => x.EmployeeId == null && x.Allocated == false));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task CancelShift_Shift_GetShiftRepoHitCorrectly()
        {
            // Arrange
            DateTime now = DateTime.Now;
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });

            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            await controller.CancelShift(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get(
                Arg.Is<string>("SELECT * FROM c WHERE c.LocationId = @locationId AND c.StartDateTime = @start AND c.EndDateTime = @end AND c.WorkType = @workType AND c.Allocated = true AND c.EmployeeId = @employeeId"), 
                Arg.Any<Dictionary<string, object>>(), 
                Arg.Any<string>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public void Book_Attributes_HasEmployeePolicy()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("Book").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgBEmployee", attribute.Policy);
        }

        [Fact]
        public async Task Book_NoIdClaim_Exception()
        {
            // Arrange
            DateTime now = DateTime.Now;
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.Book(viewModel));
            Assert.Equal("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required", exception.Message);
        }

        [Fact]
        public async Task Book_EmployeeNotOnShift_ShiftRepoHitWithCorrectQuery()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer); controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            await controller.Book(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Get(
                "SELECT * FROM c WHERE c.EmployeeId = @employeeId",
                Arg.Any<Dictionary<string, object>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Book_EmployeeOnShift_ViewDataError()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            shiftRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now, LocationId = "1", Allocated = true, EmployeeId = "abc" },
                new Library.Models.Shift { StartDateTime = now.Add(new TimeSpan(1,0,0,0)), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer); controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            IActionResult result = await controller.Book(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("You are already booked to work on this day.", viewResult.ViewData["ValidationError"].ToString());
        }

        [Fact]
        public async Task Book_EmployeeNotOnShift_BooksOnShift()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId = @locationId AND c.StartDateTime = @start AND c.EndDateTime = @end AND c.WorkType = @workType AND c.Allocated = false", 
                Arg.Any<IDictionary<string, object>>(), Arg.Any<string>()).Returns(new[]
            {
                new Library.Models.Shift { StartDateTime = now.AddDays(1), LocationId = "1", Allocated = true, EmployeeId = "xyz" }

            });
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer); controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            await controller.Book(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(1).Update(Arg.Is<Library.Models.Shift>(x => x.EmployeeId == "123" && x.Allocated == true));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Book_EmployeeNotOnShiftButShiftNotAvailable_ViewDataWarning()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            DateTime now = DateTime.Now;
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer); controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));
            ShiftViewModel viewModel = new ShiftViewModel { LocationName = "Contoso", Start = now, End = now.AddHours(9), WorkType = "Till" };

            // Act
            IActionResult result = await controller.Book(viewModel);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("No available shifts at this time.", viewResult.ViewData["ValidationError"].ToString());
        }

        [Fact]
        public void Add_AuthAttribute_HasManagerRole()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("Add").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task Add_NoParams_LocationRepoHit()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();          
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            // Act
            await controller.Add();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            locationRepository.Received(1).Get();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Add_NoParams_SearchShiftViewModel()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);
            // Act
            IActionResult result = await controller.Add();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<AddShiftViewModel>(viewResult.Model);
        }

        [Fact]
        public void AddShift_AuthAttribute_HasManagerRole()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("AddShift").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task AddShift_ViewModel_ShiftRepoHitForEachShift()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer); AddShiftViewModel viewModel = new AddShiftViewModel
            {
                NewShift = new ShiftViewModel
                {
                    Start = DateTime.Now,
                    Quantity = 2,
                    WorkType = "Till",
                    LocationName = "Contoso",
                }
            };

            // Act
            await controller.AddShift(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(2).Add(Arg.Any<Library.Models.Shift>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public void Upload_AuthAttribute_HasManagerRole()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("Upload").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task Upload_NoParams_FileUploadViewModel()
        {
            // Arrange
            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get().Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer);

            // Act
            IActionResult result = await controller.Upload();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            FileUploadViewModel viewModel = Assert.IsType<FileUploadViewModel>(viewResult.Model);
            Assert.Contains<string>("Contoso", viewModel.LocationNames.Select(x => x.Text));
            Assert.Contains<string>("Fabrikam", viewModel.LocationNames.Select(x => x.Text));
        }

        [Fact]
        public void UploadShifts_AuthAttribute_HasManagerRole()
        {
            AuthorizeAttribute attribute = typeof(ShiftsController).GetMethod("UploadShifts").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task UploadShifts_ViewModel_ShiftRepoHit()
        {
            // Arrange
            IFormFile formFile = Substitute.For<IFormFile>();
            formFile.OpenReadStream()
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes("Start,End,WorkType\n2020-10-10,2020-10-10,Till\n2020-10-10,2020-10-10,Till")));

            IRepository<Library.Models.Shift> shiftRepository = Substitute.For<IRepository<Library.Models.Shift>>();
            IRepository<Library.Models.Location> locationRepository = Substitute.For<IRepository<Library.Models.Location>>();
            locationRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(new[] { new Library.Models.Location { id = "1", Name = "Contoso" }, new Library.Models.Location { id = "2", Name = "Fabrikam" } });

            IGraphServiceClient graphClient = Substitute.For<IGraphServiceClient>();
            IStringLocalizer<ShiftsController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftsController>>();
            ShiftsController controller = new ShiftsController(shiftRepository, locationRepository, graphClient, stringLocalizer); FileUploadViewModel viewModel = new FileUploadViewModel
            {
                LocationName = "Contoso",
                FormFile = formFile,
            };

            // Act
            await controller.UploadShifts(viewModel);

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            shiftRepository.Received(2).Add(
                Arg.Is<Library.Models.Shift>(x => x.LocationId == "1" && x.WorkType == "Till" && x.Allocated == false));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}
