using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
using System.Threading.Tasks;

namespace Project.Zap.Controllers
{
    [Authorize(Policy = "ShiftViewer")]
    public class ShiftController : Controller
    {
        private readonly IRepository<Shift> shiftRepository;
        private readonly IRepository<Organization> organizationRepository;
        private readonly Microsoft.Graph.IGraphServiceClient graphServiceClient;

        public ShiftController(
            IRepository<Shift> shiftRepository, 
            IRepository<Organization> organizationRepository, 
            Microsoft.Graph.IGraphServiceClient graphServiceClient)
        {
            this.shiftRepository = shiftRepository;
            this.organizationRepository = organizationRepository;
            this.graphServiceClient = graphServiceClient;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Shift> shifts = this.shiftRepository.Get(x => x.Start > DateTime.Now);
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                StoreNames = await this.GetStoreNames(),
                Result = shifts.Map()
            };

            return View("Index", viewModel);
        }

        private async Task<SelectList> GetStoreNames()
        {
            Organization org = (await this.organizationRepository.Get()).FirstOrDefault();

            if (org == null)
            {
                throw new ArgumentException("Organization needs to be createded before adding shifts");
            }

            return new SelectList(org.Stores.Select(x => x.Name).Distinct().Select(x => new { Value = x, Text = x }), "Value", "Text");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(SearchShiftViewModel search)
        {
            IEnumerable<Shift> shifts = this.shiftRepository.Get(x => x.StoreName == search.NewShift.StoreName);
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                StoreNames = await this.GetStoreNames(),
                Result = shifts.Where(x => x.Start.DayOfYear == search.Start.DayOfYear)
                                .Map()
                                .Where(x => search.Available ? x.Available > 0 : true)
            };

            return View("Index", viewModel);
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Delete(ShiftViewModel viewModel)
        {
            await this.shiftRepository.Delete(x => x.StoreName == viewModel.StoreName && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType);
            return await this.Index();
        }

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public IActionResult ViewShifts()
        {
            Claim id = HttpContext.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required ");
            }

            IEnumerable<Shift> shifts = this.shiftRepository.Get(x => x.EmployeeId == id.Value && x.Start > DateTime.Now).AsEnumerable();
            if (shifts?.Any() == false)
            {
                ViewData["NoShifts"] = "You have no shifts booked.";
            }
            return View("ViewShifts", shifts.Map());
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public IActionResult ViewShift(ShiftViewModel viewModel)
        {

            IEnumerable<Shift> shifts = this.shiftRepository.Get(x => x.StoreName == viewModel.StoreName && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType).Where(x => x.EmployeeId != null);
            List<Microsoft.Graph.User> list = new List<Microsoft.Graph.User>();

            if (shifts.Count() == 0)
            {
                ViewData["NoEmployees"] = "No employees are booked for this shift.";
            }

            foreach (var shift in shifts)
            {
                list.Add(graphServiceClient.Users[shift.EmployeeId].Request().GetAsync().Result);
            }

            ViewData["Employees"] = list;

            return View(viewModel);

        }

        [HttpGet]
        [Authorize(Policy = "OrgBEmployee")]
        public async Task<IActionResult> CancelShift(ShiftViewModel viewModel)
        {
            Claim id = HttpContext.User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault();

            if (id == null)
            {
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required ");
            }

            Shift shift = this.shiftRepository.Get(x => x.StoreName == viewModel.StoreName && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType && x.Allocated == true).FirstOrDefault();

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
                throw new ArgumentException("http://schemas.microsoft.com/identity/claims/objectidentifier claim is required ");
            }

            IEnumerable<Shift> bookedShifts = this.shiftRepository.Get(x => x.EmployeeId == id.Value);
            if(bookedShifts?.Where(x => x.Start.DayOfYear == viewModel.Start.DayOfYear && x.Start.Year == viewModel.Start.Year).FirstOrDefault() != null)
            {
                ViewData["ValidationError"] = "You are already booked to work on this day.";
                return await this.Index();
            }

            Shift storeShift = this.shiftRepository.Get(x => x.StoreName == viewModel.StoreName && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType && x.Allocated == false).FirstOrDefault();

            if (storeShift == null)
            {
                ViewData["ValidationError"] = "No available shifts at this time.";
                return await this.Index();
            }

            storeShift.EmployeeId = id.Value;
            storeShift.Allocated = true;

            await this.shiftRepository.Update(storeShift);

            return this.ViewShifts();
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Add()
        {
            SearchShiftViewModel viewModel = new SearchShiftViewModel
            {
                StoreNames = await this.GetStoreNames(),
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

            List<Shift> shifts = viewModel.NewShift.Map().ToList();

            shifts.ForEach(async x => await this.shiftRepository.Add(x));

            return await this.Index();
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public async Task<IActionResult> Upload()
        {
            return View(new FileUploadViewModel { StoreNames = await this.GetStoreNames() });
        }

        [HttpPost]
        [Authorize(Policy = "OrgAManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadShifts(FileUploadViewModel file)
        {
            bool headersProcessed = false;

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
                        StoreName = file.StoreName,
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
