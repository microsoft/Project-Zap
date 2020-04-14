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
    public class ShiftController : Controller
    {
        private readonly IRepository<Shift> shiftRepository;
        private readonly IRepository<Organization> organizationRepository;
        private readonly Microsoft.Graph.IGraphServiceClient graphServiceClient;

        public ShiftController(IRepository<Shift> shiftRepository, IRepository<Organization> organizationRepository, Microsoft.Graph.IGraphServiceClient graphServiceClient)
        {
            this.shiftRepository = shiftRepository;
            this.organizationRepository = organizationRepository;
            this.graphServiceClient = graphServiceClient;

        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Shift> shifts = await this.shiftRepository.Get();
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
                Result = shifts.Where(x => x.Start.DayOfYear == search.NewShift.Start.DayOfYear)
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

            IEnumerable<Shift> shifts = this.shiftRepository.Get(x => x.EmployeeId == id.Value).AsEnumerable();
            if (shifts?.Any() == false)
            {
                ViewData["NoShifts"] = "You have no shifts booked.";
            }
            return View(shifts.Map());
        }

        [HttpGet]
        [Authorize(Policy = "OrgAManager")]
        public IActionResult ViewShift(ShiftViewModel viewModel)
        {
            
            Shift shift = this.shiftRepository.Get(x => x.StoreName == viewModel.StoreName && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType).FirstOrDefault();

            if (shift.EmployeeId != null)
            {
                var employee = graphServiceClient.Users[shift.EmployeeId].Request().GetAsync().Result;
                ViewData["Employee"] = employee.GivenName + " " + employee.Surname;
            }

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

            Shift storeShift = this.shiftRepository.Get(x => x.StoreName == viewModel.StoreName && x.Start == viewModel.Start && x.End == viewModel.End && x.WorkType == viewModel.WorkType && x.Allocated == false).FirstOrDefault();

            if (storeShift == null)
            {
                ViewData["ValidationError"] = "No available shifts at this time.";
                return await this.Index();
            }

            storeShift.EmployeeId = id.Value;
            storeShift.Allocated = true;

            await this.shiftRepository.Update(storeShift);

            return await this.Index();
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
