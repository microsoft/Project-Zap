using Project.Zap.Library.Models;
using Project.Zap.Models;
using System.Collections.Generic;
using System.Linq;

namespace Project.Zap.Helpers
{
    public static class ShiftModelHelper
    {
        public static IEnumerable<ShiftViewModel> Map(this IEnumerable<Shift> shifts, IEnumerable<Location> locations)
        {
            if(!shifts.Any())
            {
                return new List<ShiftViewModel>();
            }
            var viewModels = new List<ShiftViewModel>();

            var grouped = shifts.GroupBy(x => new { Start = x.StartDateTime, End = x.EndDateTime, WorkType = x.WorkType, StoreId = x.LocationId });

            foreach(var shift in grouped)
            {
                ShiftViewModel viewModel = Map(shift.First(), locations.Where(x => x.id == shift.First().LocationId).FirstOrDefault());
                viewModel.Quantity = shift.Count();
                viewModel.Available = shift.Count(x => !x.Allocated);
                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        private static ShiftViewModel Map(Shift shift, Location location)
        {
            var viewModel = new ShiftViewModel
            {
                LocationName = location.Name,
                Start = shift.StartDateTime,
                End = shift.EndDateTime,
                WorkType = shift.WorkType,                
            };

            if(location?.Address?.Point?.coordinates != null)
            viewModel.Point = new PointViewModel
            {
                Coordinates = location.Address.Point.coordinates
            };
            return viewModel;
        }

        public static IEnumerable<Shift> Map(this ShiftViewModel viewModel, string locationId)
        {
            IList<Shift> shifts = new List<Shift>();

            for(int i = 0; i < viewModel.Quantity; i++)
            {
                shifts.Add(new Shift
                {
                    LocationId = locationId,
                    StartDateTime = viewModel.Start,
                    EndDateTime = viewModel.End,
                    WorkType = viewModel.WorkType
                });
            }

            return shifts;
        }
    }
}
