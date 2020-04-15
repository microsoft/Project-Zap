using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Project.Zap.Filters
{
    public class DateLessThanAttribute : ValidationAttribute
    {
        public string PropertyName { get; private set; }

        public string GetErrorMessageKey() => "StartLessThanEnd";

        public DateLessThanAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;
            DateTime currentValue = (DateTime)value;

            PropertyInfo property = validationContext.ObjectType.GetProperty(this.PropertyName);

            if (property == null)
            {
                throw new ArgumentException("Property with this name not found");
            }                

            DateTime comparisonValue = (DateTime)property.GetValue(validationContext.ObjectInstance);

            if (currentValue > comparisonValue)
            {
                return new ValidationResult(ErrorMessage);
            }                

            return ValidationResult.Success;
        }
    }
}
