using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "ShiftViewer")]
    public class ShiftsController : Controller
    {
        private readonly IRepository<Shift> shiftRepository;
        private readonly IRepository<Location> locationRepository;
        private readonly Microsoft.Graph.IGraphServiceClient graphServiceClient;
        private readonly IStringLocalizer<ShiftsController> stringLocalizer;
        private readonly IConfiguration configuration;
        private readonly IMapService mapService;

        public ShiftsController(
            IRepository<Shift> shiftRepository,
            IRepository<Location> locationRepository,
            Microsoft.Graph.IGraphServiceClient graphServiceClient,
            IStringLocalizer<ShiftsController> stringLocalizer,
            IConfiguration configuration,
            IMapService mapService)
        {
            this.shiftRepository = shiftRepository;
            this.locationRepository = locationRepository;
            this.graphServiceClient = graphServiceClient;
            this.stringLocalizer = stringLocalizer;
            this.configuration = configuration;
            this.mapService = mapService;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Location> locations = await this.locationRepository.Get();
            if (locations == null || !locations.Any())
            {
                return Redirect("/Locations");
            }

            IEnumerable<Shift> shifts = await this.shiftRepository.Get("SELECT * FROM c WHERE c.StartDateTime > @start", new Dictionary<string, object> { { "@start", DateTime.Now } });
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                LocationNames = this.GetLocationNames(locations),
                Result = shifts.Map(locations)
            };

            ViewData["AzureMapsKey"] = this.configuration["AzureMapsSubscriptionKey"];
            return View("Index", viewModel);
        }

        private SelectList GetLocationNames(IEnumerable<Location> locations)
        {
            return new SelectList(locations.Select(x => x.Name).Distinct().Select(x => new { Value = x, Text = x }), "Value", "Text");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(SearchShiftViewModel search)
        {
            IEnumerable<Location> locations = await this.locationRepository.Get();

            List<string> locationIds = new List<string>();

            if (search.Locations != null && search.Locations.Any())
            {
                locationIds.AddRange(locations.Where(x => search.Locations.Contains(x.Name)).Select(x => x.id).ToList());
            } 
            if(!string.IsNullOrWhiteSpace(search.DistanceInMeters))
            {
                Point point = await this.mapService.GetCoordinates(new Address { ZipOrPostcode = search.ZipOrPostcode });
                IEnumerable<Location> filteredLocations = await this.locationRepository.Get(
                    "SELECT * FROM c WHERE ST_DISTANCE(c.Address.Point, {'type': 'Point', 'coordinates':[@lat, @lon]}) < @distance",
                    new Dictionary<string, object>
                    {
                        { "@lat", point.coordinates[0] },
                        { "@lon", point.coordinates[1] },
                        { "@londistance", search.DistanceInMeters }
                    });
                locationIds.AddRange(filteredLocations.Select(x => x.id));
            }

            IEnumerable <Shift> shifts = await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.StartDateTime >= @startDateTime AND ARRAY_CONTAINS(@locationIds, c.LocationId)",
                new Dictionary<string, object>
                {
                    { "@startDateTime", search.Start },
                    { "@locationIds", locationIds }
                });

            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                LocationNames = this.GetLocationNames(locations),
                Result = shifts.Map(locations).Where(x => search.Available ? x.Available > 0 : true)
            };

            ViewData["AzureMapsKey"] = this.configuration["AzureMapsSubscriptionKey"];
            return View("Index", viewModel);
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Delete(ShiftViewModel viewModel)
        {
            Location location = await this.GetLocation(viewModel.LocationName);
            await this.shiftRepository.Delete(x => x.LocationId == location.id && x.StartDateTime == viewModel.Start && x.EndDateTime == viewModel.End && x.WorkType == viewModel.WorkType);
            return await this.Index();
        }

        private async Task<Location> GetLocation(string name) => (await this.locationRepository.Get("SELECT * FROM c WHERE c.Name = @name", new Dictionary<string, object> { { "@name", name } })).FirstOrDefault();

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public async Task<IActionResult> ViewShifts()
        {
            Claim id = HttpContext?.User?.Claims?.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
            }

            IEnumerable<Shift> shifts = await this.shiftRepository.Get(
               "SELECT * FROM c WHERE c.EmployeeId = @employeeId AND c.StartDateTime > @start",
               new Dictionary<string, object> { { "@employeeId", id.Value }, { "@start", DateTime.Now } });

            if (shifts?.Any() == false)
            {
                ViewData["NoShifts"] = this.stringLocalizer["NoShifts"];
            }
            return View("ViewShifts", shifts.Map(await this.locationRepository.Get()));
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> ViewShift(ShiftViewModel viewModel)
        {
            Location location = await this.GetLocation(viewModel.LocationName);
            IEnumerable<Shift> shifts = await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId = @locationId AND c.StartDateTime = @start AND c.EndDateTime = @end AND c.WorkType = @workType",
                new Dictionary<string, object>
                {
                    { "@locationId", location.id },
                    { "@start", viewModel.Start },
                    { "@end", viewModel.End },
                    { "@workType", viewModel.WorkType }
                },
                location.id);

            IEnumerable<Shift> bookedShifts = shifts.Where(x => x.EmployeeId != null);

            if (bookedShifts.Count() == 0)
            {
                ViewData["NoEmployees"] = this.stringLocalizer["NoEmployees"];
            }

            List<string> employees = new List<string>();
            foreach (var shift in bookedShifts)
            {
                Microsoft.Graph.User user = await graphServiceClient.Users[shift.EmployeeId].Request().GetAsync();
                employees.Add($"{user.GivenName} {user.Surname}");
            }

            ViewData["Employees"] = employees;

            return View(viewModel);

        }

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public async Task<IActionResult> CancelShift(ShiftViewModel viewModel)
        {
            Claim id = HttpContext.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
            }

            Location location = await this.GetLocation(viewModel.LocationName);

            Shift shift = (await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId = @locationId AND c.StartDateTime = @start AND c.EndDateTime = @end AND c.WorkType = @workType AND c.Allocated = true AND c.EmployeeId = @employeeId",
                new Dictionary<string, object>
                {
                    { "@locationId", location.id },
                    { "@start", viewModel.Start },
                    { "@end", viewModel.End },
                    { "@workType", viewModel.WorkType },
                    { "@employeeId", id.Value }
                },
                location.id)).FirstOrDefault();

            shift.EmployeeId = null;
            shift.Allocated = false;

            await this.shiftRepository.Update(shift);

            return await this.Index();
        }

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public async Task<IActionResult> Book(ShiftViewModel viewModel)
        {
            Claim id = HttpContext.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
            }

            IEnumerable<Shift> bookedShifts = await this.shiftRepository.Get("SELECT * FROM c WHERE c.EmployeeId = @employeeId", new Dictionary<string, object> { { "@employeeId", id.Value } });
            if (bookedShifts?.Where(x => x.StartDateTime.DayOfYear == viewModel.Start.DayOfYear && x.StartDateTime.Year == viewModel.Start.Year).FirstOrDefault() != null)
            {
                ViewData["ValidationError"] = "You are already booked to work on this day.";
                return await this.Index();
            }

            Location location = await this.GetLocation(viewModel.LocationName);
            Shift shift = (await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId = @locationId AND c.StartDateTime = @start AND c.EndDateTime = @end AND c.WorkType = @workType AND c.Allocated = false",
                new Dictionary<string, object>
                {
                    { "@locationId", location.id },
                    { "@start", viewModel.Start },
                    { "@end", viewModel.End },
                    { "@workType", viewModel.WorkType }
                },
                location.id)).FirstOrDefault();

            if (shift == null)
            {
                ViewData["ValidationError"] = "No available shifts at this time.";
                return await this.Index();
            }

            shift.EmployeeId = id.Value;
            shift.Allocated = true;

            await this.shiftRepository.Update(shift);

            return await this.ViewShifts();
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Add()
        {
            AddShiftViewModel viewModel = new AddShiftViewModel
            {
                LocationNames = this.GetLocationNames(await this.locationRepository.Get()),
                NewShift = new ShiftViewModel()
            };
            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Policy = "OrgAManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShift(AddShiftViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View("Add", viewModel);
            }

            Location location = await this.GetLocation(viewModel.NewShift.LocationName);
            List<Shift> shifts = viewModel.NewShift.Map(location.id).ToList();

            shifts.ForEach(async x => await this.shiftRepository.Add(x));

            return await this.Index();
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Upload()
        {
            return View(new FileUploadViewModel { LocationNames = this.GetLocationNames(await this.locationRepository.Get()) });
        }

        [HttpPost]
        [Authorize(Policy = "OrgAManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadShifts(FileUploadViewModel file)
        {
            bool headersProcessed = false;
            Location location = await this.GetLocation(file.LocationName);

            using (StreamReader reader = new StreamReader(file.FormFile.OpenReadStream()))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!headersProcessed)
                    {
                        headersProcessed = true;
                        continue;
                    }

                    string[] parts = line.Split(",");
                    await this.shiftRepository.Add(new Shift
                    {
                        LocationId = location.id,
                        StartDateTime = DateTime.Parse(parts[0]),
                        EndDateTime = DateTime.Parse(parts[1]),
                        WorkType = parts[2],
                        Allocated = false
                    });

                }
            }

            return await this.Index();
        }
    }
}
