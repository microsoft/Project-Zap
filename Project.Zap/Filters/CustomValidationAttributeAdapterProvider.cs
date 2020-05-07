using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Zap.Filters
{
    public class CustomValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
    {
        private readonly IValidationAttributeAdapterProvider baseProvider = new ValidationAttributeAdapterProvider();

        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            if (attribute is DateMustBeEqualOrGreaterThanCurrentAttribute dateMustBeEqualOrGreaterThanCurrentAttribute)
            {
                return new DateMustBeEqualOrGreaterThanCurrentAttributeAdapter(dateMustBeEqualOrGreaterThanCurrentAttribute, stringLocalizer);
            }


            if (attribute is DateLessThanAttribute dateLessThanAttribute)
            {
                return new DateLessThanAttributeAdapter(dateLessThanAttribute, stringLocalizer);
            }


            return baseProvider.GetAttributeAdapter(attribute, stringLocalizer);
        }

    }
}
