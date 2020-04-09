using Project.Zap.Library.Models;
using Project.Zap.Models;
using System.Collections.Generic;

namespace Project.Zap.Helpers
{
    public static class PartnerHelper 
    {
        public static PartnerOrganization Map(this PartnerOrganizationViewModel viewModel)
        {
            return new PartnerOrganization { Name = viewModel?.Name };
        }

        public static IEnumerable<PartnerOrganizationViewModel> Map(this IEnumerable<PartnerOrganization> partners)
        {
            List<PartnerOrganizationViewModel> viewModels = new List<PartnerOrganizationViewModel>();

            foreach(var partner in partners)
            {
                viewModels.Add(Map(partner));
            }

            return viewModels;
        }

        public static PartnerOrganizationViewModel Map (this PartnerOrganization partner)
        {
            return new PartnerOrganizationViewModel
            {
                Name = partner?.Name,
                RegistrationCode = partner?.RegistrationCode
            };
        }
    }
}
