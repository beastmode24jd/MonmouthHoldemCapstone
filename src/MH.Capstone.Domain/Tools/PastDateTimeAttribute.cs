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
                dtoValue = dateTime;
            }
            else if (value is DateTimeOffset offsetValue)
            {
                dtoValue = offsetValue;
            }
            else
            {
                // Check if the value is null or not valid and return a validation error if so
                // Originally checked: if (value is not DateTime dateTimeValue)
                return new ValidationResult($"The {validationContext.DisplayName} field must be a valid date and time.", memberNames);
            }

            var now = _useUtc ? DateTime.UtcNow : DateTime.Now;
            // Check if the date and time is in the past and return a validation error if it's not
            return dtoValue > now ? 
                new ValidationResult($"The {validationContext.DisplayName} field must be a past date and time.", memberNames) 
                : ValidationResult.Success;
        }
    }
}
