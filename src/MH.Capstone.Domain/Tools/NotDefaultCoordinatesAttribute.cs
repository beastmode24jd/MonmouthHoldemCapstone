using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.Domain.Tools
{
    /// <summary>
    /// Validates that latitude and longitude are not both set to the default value of 0.0.
    /// Apply this attribute to a class that has Latitude and Longitude properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NotDefaultCoordinatesAttribute : ValidationAttribute
    {
        public string LatitudePropertyName { get; set; } = "Latitude";
        public string LongitudePropertyName { get; set; } = "Longitude";

        public NotDefaultCoordinatesAttribute()
        {
            ErrorMessage = "Latitude and Longitude cannot both be 0. Please enter valid coordinates.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var type = value.GetType();
            
            var latProperty = type.GetProperty(LatitudePropertyName);
            var lngProperty = type.GetProperty(LongitudePropertyName);

            if (latProperty == null || lngProperty == null)
            {
                return ValidationResult.Success;
            }

            var latValue = latProperty.GetValue(value);
            var lngValue = lngProperty.GetValue(value);

            decimal latitude = Convert.ToDecimal(latValue);
            decimal longitude = Convert.ToDecimal(lngValue);

            if (latitude == 0.0m && longitude == 0.0m)
            {
                return new ValidationResult(
                    ErrorMessage,
                    new[] { LatitudePropertyName, LongitudePropertyName });
            }

            return ValidationResult.Success;
        }
    }
}