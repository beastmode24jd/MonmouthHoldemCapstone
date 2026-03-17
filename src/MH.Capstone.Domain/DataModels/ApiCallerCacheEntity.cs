using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{
    [Table("AnimalApiCache")]
    public class ApiCallerCacheEntity
    {
        [Required] public string ApiJson { get; set; } = null!;

        [Required] [MaxLength(250)] public string Url { get; set; } = null!;

        [Required] [MaxLength(500)] public string QueryParams { get; set; } = null!;

        [Required] public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;

        public static ApiCallerCacheEntity Create(string url, string queryParams, string apiJson,
            DateTimeOffset? cachedAt = null) =>
            new ApiCallerCacheEntity
            {
                Url = url,
                QueryParams = queryParams,
                ApiJson = apiJson,
                CachedAt = cachedAt ?? DateTimeOffset.UtcNow
            };
    }
}