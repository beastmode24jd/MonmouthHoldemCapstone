using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Tools;

namespace MH.Capstone.WebApp.Models
{
    public class SightingUploadViewModel
    {
        public string DeviceTimezone { get; set; } = "America/Los_Angeles";
        [Required]
        [Range(-90.00000, 90.00000, ErrorMessage = "Latitude must be within -90 and 90 inclusive")]
        [DisplayFormat(DataFormatString = "{0:F5}", ApplyFormatInEditMode = true)]
        public decimal Latitude { get; set; } = 0.0m;

        [Required]
        [Range(-180.00000, 180.00000, ErrorMessage = "Longitude must be within -180 and 180 inclusive")]
        [DisplayFormat(DataFormatString = "{0:F5}", ApplyFormatInEditMode = true)]
        public decimal Longitude { get; set; } = 0.0m;

        [MaxLength(500)]
        public string? Description { get; set; } = string.Empty;

        [Required]
        //[Range(1, 2 * (1024 * 1024))]
        public IFormFile? UploadedImage { get; set; }

        [Required]
        [PastDateTime]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        public SightingUploadViewModel() {}

        public SightingUploadViewModel(DateTimeOffset timestamp, decimal latitude, decimal longitude, string? description, string deviceTimezone = "America/Los_Angeles")
        {
            Timestamp = timestamp;
            Latitude = latitude;
            Longitude = longitude;
            Description = description;
            DeviceTimezone = deviceTimezone;
        }
    }

    internal static class SightingsModelExtensions
    {
        internal static Sighting ToDataModel(this SightingUploadViewModel vm, Guid userId)
        {
            return new Sighting
            {
                Id = Guid.Empty, // So EF will generate a new ID when saving (Add)
                UserId = userId,
                // If Sighting expects DateTime, convert:
                Timestamp = (vm.Timestamp is DateTimeOffset dto) ? dto.UtcDateTime : vm.Timestamp.DateTime,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                Description = vm.Description,
                ImageBuffer = vm.UploadedImage?.ToByteArray() ?? []
            };
        }
    }
}
