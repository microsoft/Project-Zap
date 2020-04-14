using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Project.Zap.Filters
{
    public class DateLessThanAttributeAdapter : AttributeAdapterBase<DateLessThanAttribute>
    {
        private readonly IStringLocalizer stringLocalizer;

        public DateLessThanAttributeAdapter(DateLessThanAttribute attribute, IStringLocalizer stringLocalizer) : base(attribute, stringLocalizer)
        {
            this.stringLocalizer = stringLocalizer;
        }
        public override void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-datelessthan", GetErrorMessage(context));

            string propertyName = Attribute.PropertyName;
            MergeAttribute(context.Attributes, "data-val-datelessthan-property", propertyName);
        }

        public override string GetErrorMessage(ModelValidationContextBase validationContext) => this.stringLocalizer[Attribute.GetErrorMessageKey()];
    }
}
