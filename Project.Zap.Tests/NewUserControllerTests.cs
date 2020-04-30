using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using NSubstitute;
using Project.Zap.Controllers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Project.Zap.Tests
{
    public class NewUserControllerTests
    {
        [Fact]
        public async Task Register_InvalidModel_BadRequest()
        {
            // Arrange
            NewUserController controller = this.GetController();
            controller.ModelState.AddModelError("Bad", "Error");

            // Act
            IActionResult result = await controller.Register(new Models.NewUserViewModel());

            // Assert
            BadRequestObjectResult badRequest = Assert.IsAssignableFrom<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Class_Attributes_HasAuth()
        {
            AuthorizeAttribute attribute = typeof(NewUserController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
        }

        [Fact]
        public async Task Register_NoIdClaim_Exception()
        {
            // Arrange
            NewUserController controller = this.GetController();

            // Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => controller.Register(new Models.NewUserViewModel()));
            Assert.Equal("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required", exception.Message);
        }

        [Fact]
        public async Task Register_WithManagerCode_GraphApiWithManagerRole()
        {
            // Arrange 
            IGraphServiceClient graphServiceClient = Substitute.For<IGraphServiceClient>();
            IConfiguration configuration = Substitute.For<IConfiguration>();
            configuration["ManagerCode"].Returns("123456");
            configuration["ExtensionId"].Returns("abc");
            NewUserController controller = this.GetController(graphServiceClient: graphServiceClient, configuration: configuration);
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            await controller.Register(new Models.NewUserViewModel { RegistrationCode = "123456" });

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            graphServiceClient.Users["123"].Request().Received(1).UpdateAsync(
                Arg.Is<User>(user => user.AdditionalData.Contains(new KeyValuePair<string, object>("extension_abc_zaprole", "org_a_manager"))));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Register_WithPartnerCode_GraphApiWithEmployeeRole()
        {
            // Arrange 
            IGraphServiceClient graphServiceClient = Substitute.For<IGraphServiceClient>();
            IRepository<PartnerOrganization> partnerRepository = Substitute.For<IRepository<PartnerOrganization>>();
            partnerRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(new[] { new PartnerOrganization { RegistrationCode = "654321" } });
            IConfiguration configuration = Substitute.For<IConfiguration>();
            configuration["ManagerCode"].Returns("123456");
            configuration["ExtensionId"].Returns("abc");
            NewUserController controller = this.GetController(graphServiceClient: graphServiceClient, configuration: configuration, partnerRepository: partnerRepository);
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            await controller.Register(new Models.NewUserViewModel { RegistrationCode = "654321" });

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            graphServiceClient.Users["123"].Request().Received(1).UpdateAsync(
                Arg.Is<User>(user => user.AdditionalData.Contains(new KeyValuePair<string, object>("extension_abc_zaprole", "org_b_employee"))));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Register_WithWrongCode_ViewModelContainsError()
        {
            // Arrange 
            IGraphServiceClient graphServiceClient = Substitute.For<IGraphServiceClient>();
            IConfiguration configuration = Substitute.For<IConfiguration>();
            configuration["ManagerCode"].Returns("123456");
            configuration["ExtensionId"].Returns("abc");
            NewUserController controller = this.GetController(graphServiceClient: graphServiceClient, configuration: configuration);
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            IActionResult result = await controller.Register(new Models.NewUserViewModel { RegistrationCode = "wrongcode" });

            // Assert
            ViewResult viewResult = Assert.IsAssignableFrom<ViewResult>(result);
            Assert.Contains<string>("ErrorMessage", viewResult.ViewData.Keys);
        }


        [Fact]
        public async Task Register_WithCode_RedirectToSignOutOnSuccess()
        {
            // Arrange 
            IRepository<PartnerOrganization> partnerRepository = Substitute.For<IRepository<PartnerOrganization>>();
            partnerRepository.Get(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(new[] { new PartnerOrganization { RegistrationCode = "654321" } });
            IConfiguration configuration = Substitute.For<IConfiguration>();
            configuration["ManagerCode"].Returns("123456");
            configuration["ExtensionId"].Returns("abc");
            NewUserController controller = this.GetController(configuration: configuration, partnerRepository: partnerRepository);
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "123") }));

            // Act
            IActionResult result = await controller.Register(new Models.NewUserViewModel { RegistrationCode = "654321" });

            // Assert
            RedirectResult viewResult = Assert.IsAssignableFrom<RedirectResult>(result);
            Assert.Equal("/AzureADB2C/Account/SignOut", viewResult.Url);
        }

        private NewUserController GetController(
        IGraphServiceClient graphServiceClient = null,
        IRepository<PartnerOrganization> partnerRepository = null,
        IStringLocalizer<NewUserController> stringLocalizer = null,
        IConfiguration configuration = null,
        ILogger<NewUserController> logger = null)

        {
            graphServiceClient = graphServiceClient ?? Substitute.For<IGraphServiceClient>();
            partnerRepository = partnerRepository ?? Substitute.For<IRepository<PartnerOrganization>>();
            stringLocalizer = stringLocalizer ?? Substitute.For<IStringLocalizer<NewUserController>>();
            configuration = configuration ?? Substitute.For<IConfiguration>();
            logger = logger ?? Substitute.For<ILogger<NewUserController>>();

            var controller = new NewUserController(graphServiceClient, partnerRepository, stringLocalizer, configuration, logger);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            return controller;
        }
    }    
}
