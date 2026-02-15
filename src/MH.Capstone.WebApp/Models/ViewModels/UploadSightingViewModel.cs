using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Tools;

namespace MH.Capstone.WebApp.Models.ViewModels
{
    public class UploadSightingViewModel
    {
        [Required]
        [PastDateTime]
        public DateTime Timestamp { get; set; }

        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be within -90 and 90 inclusive")]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be within -180 and 180 inclusive")]
        public decimal Longitude { get; set; }

        [MaxLength(500)] 
        public string? Description { get; set; } = string.Empty;

        public UploadSightingViewModel() {}

        public UploadSightingViewModel(DateTime timestamp, decimal latitude, decimal longitude, string? description)
        {
            Timestamp = timestamp;
            Latitude = latitude;
            Longitude = longitude;
            Description = description;
        }
    }

    internal static class SightingsModelExtensions
    {
        internal static Sighting ToDataModel(this UploadSightingViewModel vm, Guid userId)
        {
            return new Sighting
            {
                Id = Guid.Empty, // So EF will generate a new ID when saving (Add)
                UserId = userId,
                Timestamp = vm.Timestamp.ToUniversalTime(),
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                Description = vm.Description
            };
        }
    }
}
