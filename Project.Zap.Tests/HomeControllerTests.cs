using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.Zap.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Project.Zap.Tests
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_AuthAttribute_EmptyClaim()
        {
            // Arrange
            HomeController controller = new HomeController();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            IActionResult result = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_AuthAttribute_HasManagerRole()
        {
            // Arrange
            HomeController controller = new HomeController();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("extension_zaprole", "org_a_manager") }));

            // Act
            IActionResult result = controller.Index();

            // Assert
            RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/Shifts", redirectResult.Url);

        }


        [Fact]
        public void Index_AuthAttribute_HasEmployeeRole()
        {
            // Arrange
            HomeController controller = new HomeController();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("extension_zaprole", "org_b_employee") }));

            // Act
            IActionResult result = controller.Index();

            // Assert
            RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/Shifts/ViewShifts", redirectResult.Url);

        }

    }
}
