using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MH.Capstone.Domain.ApiContracts.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MH.Capstone.Domain.Tools;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MH.Capstone.Domain.DataModels
{
    [Table("AnimalApiCache")]
    public class NinjaAnimalCacheEntity : IApiCallerCacheEntity<AnimalApiDto, NinjaAnimalCacheEntity>
    {
        public string Url { get; set; } = null!;

        public string QueryParams { get; set; } = null!;

        public DateTimeOffset CachedAt { get; set; }

        public AnimalApiDto CachedResponse { get; set; } = null!;

        public static NinjaAnimalCacheEntity Create(string url, AnimalApiDto apiResponse, 
            params IEnumerable<KeyValuePair<string, string>>? queryParams)
        {
            var queryParamsStr = HttpHelperMethods.CreateQueryParamsFragment(queryParams);
            return IApiCallerCacheEntity<AnimalApiDto, NinjaAnimalCacheEntity>.Create(url, apiResponse, queryParamsStr);
        }
    }

    // Separate configuration class for NinjaAnimalCacheEntity. Kept in this file per request.
    public class NinjaAnimalCacheEntityConfiguration : IEntityTypeConfiguration<NinjaAnimalCacheEntity>
    {
        public void Configure(EntityTypeBuilder<NinjaAnimalCacheEntity> builder)
        {
            // Map the CachedResponse object to a single JSON column using a value converter.
            var converter = new ValueConverter<AnimalApiDto, string>(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<AnimalApiDto>(v)!
            );

            builder.Property(p => p.CachedResponse)
                .HasConversion(converter)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            // Configure scalar properties constraints based on interface attributes
            builder.Property(p => p.Url).IsRequired().HasMaxLength(250);
            builder.Property(p => p.QueryParams).IsRequired().HasMaxLength(500);
            builder.Property(p => p.CachedAt).IsRequired();

            // Define a composite primary key so EF can create migrations for this entity.
            builder.HasKey(p => new { p.Url, p.QueryParams });
        }
    }

    // Marker interface to allow compile-time constraints without specifying generic type arguments
    public interface IApiCallerCacheEntity
    {
    }

    public interface IApiCallerCacheEntity<TApiDto, out TCacheEntity> : IApiCallerCacheEntity
        where TApiDto : class
        where TCacheEntity : class, IApiCallerCacheEntity<TApiDto, TCacheEntity>, new()
    {
        public string Url { get; set; }

        public string QueryParams { get; set; }

        public DateTimeOffset CachedAt { get; set; }

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