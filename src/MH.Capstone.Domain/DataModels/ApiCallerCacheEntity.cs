using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MH.Capstone.Domain.ApiContracts.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MH.Capstone.Domain.Tools;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MH.Capstone.Domain.DataModels
{
    [Table("AnimalApiCache")]
    public class NinjaAnimalCacheEntity : IApiCallerCacheEntity<AnimalApiDto, NinjaAnimalCacheEntity>
    {
        public string Url { get; set; } = null!;

        public string QueryParams { get; set; } = null!;

        public DateTimeOffset CachedAt { get; set; }

        // Changed to a collection to support multiple cached DTOs per cache key
        public List<AnimalApiDto> CachedResponses { get; set; } = new();

        public static NinjaAnimalCacheEntity Create(string url, AnimalApiDto apiResponse, 
            params IEnumerable<KeyValuePair<string, string>>? queryParams)
        {
            var queryParamsStr = HttpHelperMethods.CreateQueryParamsFragment(queryParams);
            var entity = IApiCallerCacheEntity<AnimalApiDto, NinjaAnimalCacheEntity>.Create(url, apiResponse, queryParamsStr);
            return entity;
        }

        // Implement IEntityTypeConfiguration<T> by delegating to the extracted configuration class
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<NinjaAnimalCacheEntity> builder)
        {
            new NinjaAnimalCacheEntityConfiguration().Configure(builder);
        }
    }

    // Separate configuration class for NinjaAnimalCacheEntity. Kept in this file per request.
    public class NinjaAnimalCacheEntityConfiguration : IEntityTypeConfiguration<NinjaAnimalCacheEntity>
    {
        public void Configure(EntityTypeBuilder<NinjaAnimalCacheEntity> builder)
        {
            // Map the CachedResponses collection to a single JSON column using a value converter.
            var converter = new ValueConverter<List<AnimalApiDto>, string>(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<AnimalApiDto>>(v) ?? new List<AnimalApiDto>()
            );

            // ValueComparer to ensure EF Core can compare the lists for change tracking
            var comparer = new ValueComparer<List<AnimalApiDto>>(
                (l1, l2) => ReferenceEquals(l1, l2) || (l1 != null && l2 != null && l1.SequenceEqual(l2)),
                l => l == null ? 0 : l.Count,
                l => l == null ? new List<AnimalApiDto>() : new List<AnimalApiDto>(l)
            );

            var prop = builder.Property(p => p.CachedResponses)
                .HasConversion(converter)
                .HasColumnName("CachedResponse")
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            // Attach the comparer to the property metadata so EF can detect changes correctly
            prop.Metadata.SetValueComparer(comparer);

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

    public interface IApiCallerCacheEntity<TApiDto, TCacheEntity> : IApiCallerCacheEntity, IEntityTypeConfiguration<TCacheEntity>
        where TApiDto : class
        where TCacheEntity : class, IApiCallerCacheEntity<TApiDto, TCacheEntity>, new()
    {
        public string Url { get; set; }

        public string QueryParams { get; set; }

        public DateTimeOffset CachedAt { get; set; }

        // Changed to a collection to represent 1-to-many of cached DTOs
        public List<TApiDto> CachedResponses { get; set; }

        public static abstract TCacheEntity Create(string url, TApiDto apiResponse,
            params IEnumerable<KeyValuePair<string, string>>? queryParams);

        public static TCacheEntity Create(string url, TApiDto apiResponse, string? queryParamsStr)
            => new TCacheEntity
            {
                Url = url,
                QueryParams = queryParamsStr ?? string.Empty,
                CachedAt = DateTimeOffset.UtcNow,
                CachedResponses = new List<TApiDto> { apiResponse }
            };
    }
}