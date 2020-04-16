using Project.Zap.Library.Models;
using Project.Zap.Models;
using System.Collections.Generic;

namespace Project.Zap.Helpers
{
    public static class LocationModelHelper
    {
        public static IEnumerable<LocationViewModel> Map(this IEnumerable<Location> locations)
        {
            IList<LocationViewModel> viewModel = new List<LocationViewModel>();
            foreach(Location location in locations)
            {
                viewModel.Add(location.Map());
            }
            return viewModel;
        }

        public static Location Map(this LocationViewModel viewModel)
        {
            return new Location
            {
                Name = viewModel.Name,
                Address = new Address
                {
                    City = viewModel?.Address?.City,
                    ZipOrPostCode = viewModel?.Address?.ZipOrPostCode
                }
            };
        }

        public static LocationViewModel Map(this Location location)
        {
            return new LocationViewModel
            {
                Name = location.Name,
                Address = new AddressViewModel
                {
                    City = location?.Address?.City,
                    ZipOrPostCode = location?.Address?.ZipOrPostCode
                }
            };
        }
    }
}
