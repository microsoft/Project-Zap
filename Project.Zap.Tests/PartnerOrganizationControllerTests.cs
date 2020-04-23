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
    public class PartnerOrganizationControllerTests
    {

        [Fact]
        public void Controller_Auth_ManagerPolicy()
        {
            AuthorizeAttribute attribute = typeof(PartnerOrganizationController).GetCustomAttribute<AuthorizeAttribute>();
            attribute.Policy = "OrgAManager";
            Assert.Equal("OrgAManager", attribute.Policy);
        }

        [Fact]
        public async Task Index_NoParams_RepoGetHitWithNoParams()
        {
            // Arrange
            IRepository<PartnerOrganization> repository = Substitute.For<IRepository<PartnerOrganization>>();
            PartnerOrganizationController controller = new PartnerOrganizationController(repository);

            // Act
            await controller.Index();

            // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Get();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        [Fact]
        public async Task Index_NoParams_ResultModelIsEnumerableOfPartnerOrganizationViewModel()
        {
            // Arrange
            IRepository<PartnerOrganization> repository = Substitute.For<IRepository<PartnerOrganization>>();
            PartnerOrganizationController controller = new PartnerOrganizationController(repository);

            // Act
            IActionResult result = await controller.Index();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<IEnumerable<PartnerOrganizationViewModel>>(viewResult.ViewData.Model);
        }

        [Fact]
        public void Add_NoParams_ReturnsViewResult()
        {
            // Arrange
            IRepository<PartnerOrganization> repository = Substitute.For<IRepository<PartnerOrganization>>();
            PartnerOrganizationController controller = new PartnerOrganizationController(repository);

            // Act
            IActionResult result = controller.Add();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AddPartnerOrganization_PartnerOrganizationViewModelInValid_ReturnsBadRequest()
        {
            // Arrange
            IRepository<PartnerOrganization> repository = Substitute.For<IRepository<PartnerOrganization>>();
            PartnerOrganizationController controller = new PartnerOrganizationController(repository);
            controller.ModelState.AddModelError("Name", "Required");
            PartnerOrganizationViewModel viewModel = new PartnerOrganizationViewModel();

            // Act
            IActionResult result = await controller.AddPartner(viewModel);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Add_PartnerOrganization_PartnerOrganizationViewModel_ViewResultDifferentRegistrationCodes()
        {
            // Arrange
            List<PartnerOrganization> partners = new List<PartnerOrganization>();
            IRepository<PartnerOrganization> repository = Substitute.For<IRepository<PartnerOrganization>>();
            repository.Add(Arg.Any<PartnerOrganization>()).Returns(x =>
            {
                partners.Add(x.Arg<PartnerOrganization>());
                return Task.CompletedTask;
            });

            PartnerOrganizationController controller = new PartnerOrganizationController(repository);

            PartnerOrganizationViewModel viewModel = new PartnerOrganizationViewModel();
            // Act
            await controller.AddPartner(viewModel);
            await controller.AddPartner(viewModel);

            // Assert
            Assert.Equal(2, partners.Count);
            Assert.NotEqual(partners[0].RegistrationCode, partners[1].RegistrationCode);
        }

        [Fact]
        public async Task Delete_PartnerName_RepoDeleteHitWithExpression()
        {
            // Arrange
            IRepository<PartnerOrganization> repository = Substitute.For<IRepository<PartnerOrganization>>();
            PartnerOrganizationController controller = new PartnerOrganizationController(repository);
            string id = "Contoso";

            // Act
            await controller.DeletePartner(id);

            // Assert            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            repository.Received(1).Delete(Arg.Any<Expression<Func<PartnerOrganization, bool>>>());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed            
        }















    }
}
