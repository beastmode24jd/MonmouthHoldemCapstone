using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MH.Capstone.Domain.Tools
{
    internal static class DataAnnotationsValidator
    {
        /// <summary>
        /// This method validates an entity using data annotations and returns a boolean indicating if the validation passed or failed.
        /// If validation fails, it also returns a list of the properties that failed validation through an out parameter.
        /// </summary>
        /// <param name="entity">The entity with data annotations to validate</param>
        /// <param name="failedProps">A <see cref="IEnumerable{string}"/> of properties in <see cref="entity"/> that failed validation</param>
        /// <returns>A boolean value representing if the entity passed validation</returns>
        public static bool TryValidateEntity(this object entity, out IEnumerable<string> failedProps)
        {
            var validationContext = new ValidationContext(entity);
            var validationResults = new List<ValidationResult>();
            failedProps = [];

            // ReSharper disable once InvertIf
            if (!Validator.TryValidateObject(entity, validationContext, validationResults, validateAllProperties: true))
            {
                // Handle validation failures, e.g. add the failed prop to the failedArgs list
                failedProps = validationResults.SelectMany(r => r.MemberNames).Distinct();
                return false;
            }

            return true;
        }
    }
}
