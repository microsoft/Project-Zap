using Microsoft.AspNetCore.Mvc.Rendering;
using Project.Zap.Library.Models;
using Project.Zap.Models;
using System.Collections.Generic;
using System.Linq;

namespace Project.Zap.Helpers
{
    public static class ShiftModelHelper
    {
        public static IEnumerable<ShiftViewModel> Map(this IEnumerable<Shift> shifts)
        {
            if(!shifts.Any())
            {
                return null;
            }
            var viewModels = new List<ShiftViewModel>();

            var grouped = shifts.GroupBy(x => new { Start = x.Start, End = x.End, WorkType = x.WorkType });

            foreach(var shift in grouped)
            {
                ShiftViewModel viewModel = Map(shift.First());
                viewModel.Quantity = shift.Count();
                viewModel.Available = shift.Count(x => !x.Allocated);
                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        private static ShiftViewModel Map(Shift shift)
        {
            return new ShiftViewModel
            {
                StoreName = shift.StoreName,
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
                    StoreName = viewModel.StoreName,
                    Start = viewModel.Start,
                    End = viewModel.End,
                    WorkType = viewModel.WorkType
                });
            }

            return shifts;
        }
    }
}
