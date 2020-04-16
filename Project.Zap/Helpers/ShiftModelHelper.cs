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
                return null;
            }
            var viewModels = new List<ShiftViewModel>();

            var grouped = shifts.GroupBy(x => new { Start = x.Start, End = x.End, WorkType = x.WorkType, StoreId = x.LocationId });

            foreach(var shift in grouped)
            {
                ShiftViewModel viewModel = Map(shift.First(), locations.Where(x => x.id == shift.First().LocationId).Select(x => x.Name).FirstOrDefault());
                viewModel.Quantity = shift.Count();
                viewModel.Available = shift.Count(x => !x.Allocated);
                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        private static ShiftViewModel Map(Shift shift, string storeName)
        {
            return new ShiftViewModel
            {
                LocationName = storeName,
                Start = shift.Start,
                End = shift.End,
                WorkType = shift.WorkType,
            };
        }

        public static IEnumerable<Shift> Map(this ShiftViewModel viewModel)
        {
            IList<Shift> shifts = new List<Shift>();
            int allocated = viewModel.Quantity - viewModel.Available;
            for(int i = 0; i < viewModel.Quantity; i++)
            {
                shifts.Add(new Shift
                {
                    LocationId = viewModel.LocationName,
                    Start = viewModel.Start,
                    End = viewModel.End,
                    WorkType = viewModel.WorkType
                });
            }

            return shifts;
        }
    }
}
