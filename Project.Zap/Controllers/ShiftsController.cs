using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Project.Zap.Helpers;
using Project.Zap.Library.Models;
using Project.Zap.Library.Services;
using Project.Zap.Models;
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
        private readonly IRepository<Location> locationRepository;
        private readonly Microsoft.Graph.IGraphServiceClient graphServiceClient;

        public ShiftsController(
            IRepository<Shift> shiftRepository, 
            IRepository<Location> locationRepository, 
            Microsoft.Graph.IGraphServiceClient graphServiceClient)
        {
            this.shiftRepository = shiftRepository;
            this.locationRepository = locationRepository;
            this.graphServiceClient = graphServiceClient;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Location> locations = await this.locationRepository.Get();
            if (locations == null || !locations.Any())
            {
                return Redirect("/Locations");
            }

            IEnumerable<Shift> shifts = await this.shiftRepository.Get("SELECT * FROM c WHERE c.Start > @start", new Dictionary<string, object> { { "@start", DateTime.Now } });
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                LocationNames = this.GetLocationNames(locations),
                Result = shifts.Map(locations)
            };

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
            string locationId = locations.Where(x => x.Name == search.Location).Select(x => x.id).FirstOrDefault();

            IEnumerable<Shift> shifts = await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId == @locationId",
                new Dictionary<string, object> { { "@locationId", locationId } },
                locationId);

            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                LocationNames = this.GetLocationNames(locations),
                Result = shifts.Where(x => x.Start.DayOfYear == search.Start.DayOfYear)
                                .Map(locations)
                                .Where(x => search.Available ? x.Available > 0 : true)
            };

            return View("Index", viewModel);
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Delete(ShiftViewModel viewModel)
        {
            Location location = await this.GetLocation(viewModel.LocationName);
            await this.shiftRepository.Delete(x => x.LocationId == location.id && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType);
            return await this.Index();
        }

        private async Task<Location> GetLocation(string name) => (await this.locationRepository.Get("SELECT * FROM c WHERE c.Name == @name", new Dictionary<string, object> { { "@name", name } })).FirstOrDefault();

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
               "SELECT * FROM c WHERE c.EmployeeId = @employeeId AND c.Start > @start",
               new Dictionary<string, object> { { "@employeeId", id.Value }, { "@start", DateTime.Now } });

            if (shifts?.Any() == false)
            {
                ViewData["NoShifts"] = "You have no shifts booked.";
            }
            return View("ViewShifts", shifts.Map(await this.locationRepository.Get()));
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> ViewShift(ShiftViewModel viewModel)
        {
            Location location = await this.GetLocation(viewModel.LocationName);
            IEnumerable<Shift> shifts = await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId == @locationId AND c.Start == @start AND c.End == @end AND c.WorkType == @workType AND c.EmployeeId != null",
                new Dictionary<string, object>
                {
                    { "@locationId", location.id },
                    { "@start", viewModel.Start },
                    { "@end", viewModel.End },
                    { "@workType", viewModel.WorkType }
                },
                location.id);

            if (shifts.Count() == 0)
            {
                ViewData["NoEmployees"] = "No employees are booked for this shift.";
            }

            List<string> employees = new List<string>();
            foreach (var shift in shifts)
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
                "SELECT * FROM c WHERE c.LocationId == @locationId AND c.Start == @start AND c.End == @end AND c.WorkType == @workType AND c.Allocated == true AND c.EmployeeId == @employeeId",
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

            IEnumerable<Shift> bookedShifts = await this.shiftRepository.Get("SELECT * FROM c WHERE c.EmployeeId == @employeeId", new Dictionary<string, object> { { "@employeeId", id.Value } });
            if(bookedShifts?.Where(x => x.Start.DayOfYear == viewModel.Start.DayOfYear && x.Start.Year == viewModel.Start.Year).FirstOrDefault() != null)
            {
                ViewData["ValidationError"] = "You are already booked to work on this day.";
                return await this.Index();
            }

            Location location = await this.GetLocation(viewModel.LocationName);
            Shift shift = (await this.shiftRepository.Get(
                "SELECT * FROM c WHERE c.LocationId == @locationId AND c.Start == @start AND c.End == @end AND c.WorkType == @workType AND c.Allocated == false",
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
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                LocationNames = this.GetLocationNames(await this.locationRepository.Get()),
                NewShift = new ShiftViewModel()
            };
            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Policy = "OrgAManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShift(SearchShiftViewModel viewModel)
        {
            if(!ModelState.IsValid)
            {
                return View("Add", viewModel);
            }

            Location location = await this.GetLocation(viewModel.Location);
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
                        Start = DateTime.Parse(parts[0]),
                        End = DateTime.Parse(parts[1]),
                        WorkType = parts[2],
                        Allocated = false
                    });

                }
            }

            return await this.Index();
        }
    }
}
