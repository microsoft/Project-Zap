using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
using Project.Zap.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "ShiftViewer")]
    public class ShiftsController : Controller
    {
        private readonly IRepository<Shift> shiftRepository;
        private readonly ILocationService locationService;
        private readonly Microsoft.Graph.IGraphServiceClient graphServiceClient;
        private readonly IStringLocalizer<ShiftsController> stringLocalizer;
        private readonly IConfiguration configuration;
        private readonly IMapService mapService;
        private readonly ILogger<ShiftsController> logger;

        public ShiftsController(
            IRepository<Shift> shiftRepository,
            ILocationService locationService,
            Microsoft.Graph.IGraphServiceClient graphServiceClient,
            IStringLocalizer<ShiftsController> stringLocalizer,
            IConfiguration configuration,
            IMapService mapService,
            ILogger<ShiftsController> logger)
        {
            this.shiftRepository = shiftRepository;
            this.locationService = locationService;
            this.graphServiceClient = graphServiceClient;
            this.stringLocalizer = stringLocalizer;
            this.configuration = configuration;
            this.mapService = mapService;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(SearchShiftViewModel search = null)
        {
            IEnumerable<Location> locations = await this.locationService.Get();
            if (locations == null || !locations.Any())
            {
                this.logger.LogInformation("No locations, so redirecting to location view");
                return Redirect("/Locations");
            }

            return await this.Search(search ?? new SearchShiftViewModel(), locations);
        }

        private async Task<SearchShiftViewModel> GetShifts(IEnumerable<Location> locations, string sql, IDictionary<string, object> parameters, bool available = true, string filterLocation = null)
        {
            IEnumerable<Shift> shifts = await this.shiftRepository.Get(sql, parameters);
            IEnumerable<ShiftViewModel> shiftViewModels = shifts.Map(locations).Where(x => available ? x.Available > 0 : true);
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                LocationNames = this.GetLocationNames(locations),
                Result = shiftViewModels.Where(x => string.IsNullOrEmpty(filterLocation) ? true : x.LocationName == filterLocation),
                MapPoints = this.GetMapPoints(shiftViewModels, locations)
            };
            return viewModel;
        }

        private IEnumerable<MapPointViewModel> GetMapPoints(IEnumerable<ShiftViewModel> shifts, IEnumerable<Location> locations)
        {
            List<MapPointViewModel> mapPoints = new List<MapPointViewModel>();
            foreach(var location in shifts.GroupBy(x => x.LocationName))
            {
                Address address = locations.Where(x => x.Name == location.Key).Select(x => x.Address).FirstOrDefault();

                MapPointViewModel mapPoint = new MapPointViewModel
                {
                    Location = location.Key,
                    Address = address?.Text,
                    ZipOrPostcode = address?.ZipOrPostcode,
                    Quantity = location.Sum(x => x.Quantity),
                    Available = location.Sum(x => x.Available),
                    Point = location.Select(x => x.Point).FirstOrDefault(),
                    Start = location.Select(x => x.Start).FirstOrDefault()
                };

                mapPoints.Add(mapPoint);
            }

            return mapPoints;
        }

        private SelectList GetLocationNames(IEnumerable<Location> locations)
        {
            return new SelectList(locations.Select(x => x.Name).Distinct().Select(x => new { Value = x, Text = x }), "Value", "Text");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OnSearch(SearchShiftViewModel search)
        {
            return RedirectToAction("Index", new { Start = search.Start.ToString("yyyy-MM-ddTHH:mm"), search.Locations, search.UseMyLocation, search.Available, search.ZipOrPostcode });
        }

        private async Task<IActionResult> Search(SearchShiftViewModel search, IEnumerable<Location> locations)
        {
            List<string> locationIds = new List<string>();

            if (search.Locations != null && search.Locations.Any())
            {
                locationIds.AddRange(locations.Where(x => search.Locations.Contains(x.Name)).Select(x => x.id).ToList());
            }
            if (search.DistanceInMeters != null && !string.IsNullOrWhiteSpace(search.ZipOrPostcode))
            {
                Point point = await this.mapService.GetCoordinates(new Address { ZipOrPostcode = search.ZipOrPostcode });
                IEnumerable<Location> filteredLocations = await this.locationService.GetByDistance(point, search.DistanceInMeters.Value);
                locationIds.AddRange(filteredLocations.Select(x => x.id));
            }

            string sql = "SELECT * FROM c WHERE c.StartDateTime >= @startDateTime";
            IDictionary<string, object> parameters = new Dictionary<string, object>
                {
                    { "@startDateTime", search.Start }
                };

            if (locationIds.Any())
            {
                sql += " AND ARRAY_CONTAINS(@locationIds, c.LocationId)";
                parameters.Add("@locationIds", locationIds);
            }
            
            SearchShiftViewModel results = await this.GetShifts(locations, sql, parameters, search.Available, search.FilterByLocation);
            search.Result = results.Result;
            search.MapPoints = results.MapPoints;
            search.LocationNames = results.LocationNames;

            ViewData["AzureMapsKey"] = this.configuration["AzureMapsSubscriptionKey"];

            return View("Index", search);
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Delete(ShiftViewModel viewModel)
        {
            Location location = await this.GetLocation(viewModel.LocationName);
            await this.shiftRepository.Delete(x => x.LocationId == location.id && x.StartDateTime == viewModel.Start && x.EndDateTime == viewModel.End && x.WorkType == viewModel.WorkType);
            return Redirect("/Shifts");
        }

        private async Task<Location> GetLocation(string name) => await this.locationService.GetByName(name);

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public async Task<IActionResult> ViewShifts()
        {
            Claim id = HttpContext?.User?.Claims?.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                ArgumentException exception = new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
                this.logger.LogError(exception, "No id claim present for user");
                throw exception;
            }

            IEnumerable<Shift> shifts = await this.shiftRepository.Get(
               "SELECT * FROM c WHERE c.EmployeeId = @employeeId AND c.StartDateTime > @start",
               new Dictionary<string, object> { { "@employeeId", id.Value }, { "@start", DateTime.Now } });

            if (shifts?.Any() == false)
            {
                this.logger.LogInformation("Trying to view shifts, but shifts currently available");
                ViewData["NoShifts"] = this.stringLocalizer["NoShifts"];
            }
            return View("ViewShifts", shifts.Map(await this.locationService.Get()));
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
                this.logger.LogInformation("Trying to view shift when there are no employees");
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
                ArgumentException exception = new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
                this.logger.LogError(exception, "No id claim present for user");
                throw exception;
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

            return Redirect("/Shifts");
        }

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public async Task<IActionResult> Book(ShiftViewModel viewModel)
        {
            Claim id = HttpContext.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                ArgumentException exception = new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required");
                this.logger.LogError(exception, "No id claim present for user");
                throw exception;
            }

            IEnumerable<Shift> bookedShifts = await this.shiftRepository.Get("SELECT * FROM c WHERE c.EmployeeId = @employeeId", new Dictionary<string, object> { { "@employeeId", id.Value } });
            if (bookedShifts?.Where(x => x.StartDateTime.DayOfYear == viewModel.Start.DayOfYear && x.StartDateTime.Year == viewModel.Start.Year).FirstOrDefault() != null)
            {
                this.logger.LogInformation("Trying to book on a shift when user is already booked out for this day");

                ViewData["ValidationError"] = this.stringLocalizer["MultipleBookError"];
                IEnumerable<Location> locations = await this.locationService.Get();
                if (locations == null || !locations.Any())
                {
                    this.logger.LogInformation("No locations, so redirecting to location view");
                    return Redirect("/Locations");
                }
                ViewData["AzureMapsKey"] = this.configuration["AzureMapsSubscriptionKey"];

                return View("Index", await this.GetShifts(locations, "SELECT * FROM c WHERE c.StartDateTime > @start", new Dictionary<string, object> { { "@start", DateTime.Now } }));
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
                this.logger.LogInformation("Trying to book shift for a time when no shifts are available");

                ViewData["ValidationError"] = this.stringLocalizer["NoShiftsAvailable"];
                IEnumerable<Location> locations = await this.locationService.Get();
                if (locations == null || !locations.Any())
                {
                    this.logger.LogInformation("No locations, so redirecting to location view");
                    return Redirect("/Locations");
                }
                ViewData["AzureMapsKey"] = this.configuration["AzureMapsSubscriptionKey"];

                return View("Index", await this.GetShifts(locations, "SELECT * FROM c WHERE c.StartDateTime > @start", new Dictionary<string, object> { { "@start", DateTime.Now } }));
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
                LocationNames = this.GetLocationNames(await this.locationService.Get()),
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
                this.logger.LogError("Add shift view model is not valid");
                return View("Add", viewModel);
            }

            Location location = await this.GetLocation(viewModel.NewShift.LocationName);
            List<Shift> shifts = viewModel.NewShift.Map(location.id).ToList();

            shifts.ForEach(async x => await this.shiftRepository.Add(x));

            return Redirect("/Shifts");
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Upload()
        {
            return View(new FileUploadViewModel { LocationNames = this.GetLocationNames(await this.locationService.Get()) });
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

            return Redirect("/Shifts");
        }
    }
}
