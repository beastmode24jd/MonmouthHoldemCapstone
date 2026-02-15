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
            // Check if the value is null or not valid and return a validation error if so
            if (value is not DateTime dateTimeValue)
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a valid date and time.");
            }

            var now = _useUtc ? DateTime.UtcNow : DateTime.Now;
            // Check if the date and time is in the past and return a validation error if it's not
            return dateTimeValue > now ? 
                new ValidationResult($"The {validationContext.DisplayName} field must be a past date and time.") 
                : ValidationResult.Success;
        }
    }
}
