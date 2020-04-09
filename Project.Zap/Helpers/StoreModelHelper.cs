using Project.Zap.Library.Models;
using Project.Zap.Models;

namespace Project.Zap.Helpers
{
    public static class StoreModelHelper
    {
        public static Store Map(this StoreViewModel viewModel)
        {
            return new Store
            {
                Name = viewModel.Name,
                Address = new Address
                {
                    City = viewModel?.Address?.City,
                    ZipOrPostCode = viewModel?.Address?.ZipOrPostCode
                }
            };
        }

        public static StoreViewModel Map(this Store store)
        {
            return new StoreViewModel
            {
                Name = store.Name,
                Address = new AddressViewModel
                {
                    City = store?.Address?.City,
                    ZipOrPostCode = store?.Address?.ZipOrPostCode
                }
            };
        }
    }
}
