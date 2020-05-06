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
            var location = new Location
            {
                Name = viewModel.Name,
                Address = new Address
                {
                    Text = viewModel?.Address,
                    ZipOrPostcode = viewModel?.ZipOrPostcode,
                }
            };
            if (viewModel.Latitude != 0 && viewModel.Latitude != 0)
            {
                location.Address.Point = new Point
                {
                    coordinates = new double[] { viewModel.Latitude, viewModel.Longitude}
                };
            }
            return location;
        }


        public static Location Map(this AddLocationViewModel viewModel)
        {
            return new Location
            {
                Name = viewModel.Name,
                Address = new Address
                {
                    Text = viewModel?.Address,
                    ZipOrPostcode = viewModel?.ZipOrPostcode
                }
            };
        }

        public static LocationViewModel Map(this Location location)
        {
            var viewModel = new LocationViewModel
            {
                Name = location.Name,
                Address = location?.Address?.Text,
                ZipOrPostcode = location?.Address?.ZipOrPostcode
            };

            if (location?.Address?.Point?.coordinates != null)
                viewModel.Point = new PointViewModel
                {
                    Coordinates = location.Address.Point.coordinates
                };
            return viewModel;
        }
    }
}
