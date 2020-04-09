using Project.Zap.Library.Models;
using Project.Zap.Models;
using System.Collections.Generic;
using System.Linq;

namespace Project.Zap.Helpers
{
    public static class OrganizationModelHelper
    {
        public static OrganizationViewModel Map(this Organization organization)
        {
            var viewModel = new OrganizationViewModel
            {
                Name = organization.Name,
                Stores = new List<StoreViewModel>()
            };

            if (organization.Stores == null || !organization.Stores.Any())
            {
                return viewModel;
            }

            foreach (Store store in organization.Stores)
            {
                viewModel.Stores.Add(new StoreViewModel
                {
                    Name = store.Name,
                    Address = new AddressViewModel
                    {
                        City = store.Address.City,
                        ZipOrPostCode = store.Address.ZipOrPostCode
                    }
                });
            }

            return viewModel;

        }
    }
}
