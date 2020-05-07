using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Project.Zap.Filters
{
    public class DateMustBeEqualOrGreaterThanCurrentAttribute : ValidationAttribute
    {
        public string PropertyName { get; private set; }

        public string GetErrorMessageKey() => "DateMustBeEqualOrGreaterThanCurrent";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;
            DateTime currentValue = (DateTime)value;

            if (currentValue <= DateTime.Today)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
