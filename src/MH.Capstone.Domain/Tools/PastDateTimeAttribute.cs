using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable IDE0290

namespace MH.Capstone.Domain.Tools
{
    public class PastDateTimeAttribute : ValidationAttribute
    {
        private readonly bool _useUtc;

        public PastDateTimeAttribute(bool useUtc = true)
        {
            _useUtc = useUtc;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            string[] memberNames = [validationContext.MemberName ?? string.Empty];

            // Check both the DateTime and DateTimeOffset values.
            DateTimeOffset dtoValue;

            if (value is DateTime dateTime)
            {
                dtoValue = new DateTimeOffset(dateTime);
            }
            else if (value is DateTimeOffset offsetValue)
            {
                dtoValue = offsetValue;
            }
            else
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a valid date and time.", memberNames);
            }

            var now = _useUtc ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
            if (dtoValue > now)
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a past date and time.", memberNames);
            }
            return ValidationResult.Success;
        }
    }
}
