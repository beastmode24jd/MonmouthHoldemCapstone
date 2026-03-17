using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MH.Capstone.Domain.ApiContracts.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MH.Capstone.Domain.DataModels
{
    [Table("AnimalApiCache")]
    public class NinjaAnimalCacheEntity : IApiCallerCacheEntity<AnimalApiDto, NinjaAnimalCacheEntity>
    {
        public string Url { get; set; } = null!;

        public string QueryParams { get; set; } = null!;

        public DateTimeOffset CachedAt { get; set; }

        public AnimalApiDto CachedResponse { get; set; } = null!;

        public static NinjaAnimalCacheEntity Create(string url, AnimalApiDto apiResponse, params IEnumerable<KeyValuePair<string, string>>? queryParams)
        {
            throw new NotImplementedException();
        }

        public void Configure(EntityTypeBuilder<NinjaAnimalCacheEntity> builder)
        {
            builder.ComplexProperty(p => p.CachedResponse);
        }
    }

    // Marker interface to allow compile-time constraints without specifying generic type arguments
    public interface IApiCallerCacheEntity
    {
    }

    public interface IApiCallerCacheEntity<TApiDto, TCacheEntity> : IApiCallerCacheEntity, IEntityTypeConfiguration<TCacheEntity>
        where TApiDto : class
        where TCacheEntity : class, IApiCallerCacheEntity<TApiDto, TCacheEntity>, new()
    {
        [Required]
        [MaxLength(250)]
        public string Url { get; set; }

        [Required]
        [MaxLength(500)]
        public string QueryParams { get; set; }

        [Required]
        public DateTimeOffset CachedAt { get; set; }

        [Required]
        public TApiDto CachedResponse { get; set; }

        public static abstract TCacheEntity Create(string url, TApiDto apiResponse,
            params IEnumerable<KeyValuePair<string, string>>? queryParams);

        public static TCacheEntity Create(string url, TApiDto apiResponse, string? queryParamsStr)
            => new TCacheEntity
            {
                Url = url,
                QueryParams = queryParamsStr ?? string.Empty,
                CachedAt = DateTimeOffset.UtcNow,
                CachedResponse = apiResponse
            };
    }
}