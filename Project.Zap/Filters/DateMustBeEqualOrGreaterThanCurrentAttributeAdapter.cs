using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Zap.Filters
{
    public class DateMustBeEqualOrGreaterThanCurrentAttributeAdapter : AttributeAdapterBase<DateMustBeEqualOrGreaterThanCurrentAttribute>
    {
        private readonly IStringLocalizer stringLocalizer;

        public DateMustBeEqualOrGreaterThanCurrentAttributeAdapter(DateMustBeEqualOrGreaterThanCurrentAttribute attribute, IStringLocalizer stringLocalizer) : base(attribute, stringLocalizer)
        {
            this.stringLocalizer = stringLocalizer;
        }
        public override void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-datemustbeequalorgreaterthancurrent", GetErrorMessage(context));
        }

        public override string GetErrorMessage(ModelValidationContextBase validationContext) => this.stringLocalizer[Attribute.GetErrorMessageKey()];
    }
}
