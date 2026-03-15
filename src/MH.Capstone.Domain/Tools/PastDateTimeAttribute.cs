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

            DateTime dateTimeValue;
            if (value is DateTimeOffset dto)
            {
                dateTimeValue = dto.UtcDateTime;
            }
            else if (value is DateTime dt)
            {
                dateTimeValue = _useUtc ? dt.ToUniversalTime() : dt;
            }
            else
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a valid date and time.", memberNames);
            }

            var now = _useUtc ? DateTime.UtcNow : DateTime.Now;
            return dateTimeValue > now ?
                new ValidationResult($"The {validationContext.DisplayName} field must be a past date and time.", memberNames)
                : ValidationResult.Success;
        }
    }
}
