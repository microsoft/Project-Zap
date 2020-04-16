using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Project.Zap.Controllers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Project.Zap.Tests
{
    public class ShiftManagementControllerTests
    {
        [Fact]
        public async Task UploadShifts_CsvFile_AddShifts()
        {
            // Arrange
            IRepository<Shift> shiftRepository = Substitute.For<IRepository<Shift>>();
            IRepository<Organization> organizationRepository = Substitute.For<IRepository<Organization>>();
            Microsoft.Graph.IGraphServiceClient graphServiceClient = Substitute.For<Microsoft.Graph.IGraphServiceClient>();
            IStringLocalizer<ShiftController> stringLocalizer = Substitute.For<IStringLocalizer<ShiftController>>();


            ShiftController controller = new ShiftController(shiftRepository, organizationRepository, graphServiceClient, stringLocalizer);
            FileUploadViewModel viewModel = new FileUploadViewModel { StoreName = "Contoso" };
            string content = "Start, End, Type\n2020-04-10T09:00,2020-04-10T17:00,Shelf Stacker\n2020-04-10T10:00,2020-04-10T18:00,Tills";
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            viewModel.FormFile = new FormFile(new MemoryStream(contentBytes), 0, contentBytes.Length, null, "shifts.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "csv"
            };


            // Act
            await controller.UploadShifts(viewModel);


#pragma warning disable 4014
            // Assert
            shiftRepository.Received(1).Add(Arg.Is<Shift>(x => x.Start == DateTime.Parse("2020-04-10T09:00")));
            shiftRepository.Received(1).Add(Arg.Is<Shift>(x => x.Start == DateTime.Parse("2020-04-10T10:00")));
#pragma warning restore 4014

        }
    }
}
