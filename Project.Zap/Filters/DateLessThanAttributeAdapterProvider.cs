using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Project.Zap.Filters
{
    public class DateLessThanAttributeAdapterProvider : IValidationAttributeAdapterProvider
    {
        private readonly IValidationAttributeAdapterProvider baseProvider = new ValidationAttributeAdapterProvider();

        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            if (attribute is DateLessThanAttribute dateLessThanAttribute)
            {
                return new DateLessThanAttributeAdapter(dateLessThanAttribute, stringLocalizer);
            }

            return baseProvider.GetAttributeAdapter(attribute, stringLocalizer);
        }
    }
}
