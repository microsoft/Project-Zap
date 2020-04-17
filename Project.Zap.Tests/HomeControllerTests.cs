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
        public void Index_AuthAttribute_HasManagerRoleAsync()
        {
            // Arrange
            HomeController controller = new HomeController();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("extension_zaprole", "org_a_manager") }));

            // Act
            controller.Index();

            // Assert
            AuthorizeAttribute attribute = typeof(HomeController).GetMethod("Index").GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("OrgAManager", attribute.Policy);
        }

    }
}
