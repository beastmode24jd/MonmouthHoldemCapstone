using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.Tools
{
    public static class Extensions
    {
        public static byte[] ToByteArray(this IFormFile formFile)
        {
            ArgumentNullException.ThrowIfNull(formFile);

            using var memoryStream = new MemoryStream();
            formFile.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }

        internal static bool IsOfErrorType(this DbUpdateException ex, SqlErrorNumber errNum)
            => ex.InnerException is SqlException sqlEx && sqlEx.IsOfErrorType(errNum);

        internal static bool IsOfErrorType(this SqlException ex, SqlErrorNumber errNum) 
            => ex.Errors.Cast<SqlError>().Any(e => e.IsOfErrorType(errNum));

        internal static bool IsOfErrorType(this SqlError err, SqlErrorNumber errNum)
            => err.Number == (int)errNum;
    }

    public static class HttpHelperMethods
    {
        public static string CreateQueryParamsFragment(params IEnumerable<KeyValuePair<string, string>>? queryParams)
        {
            var queryList = queryParams?.ToList() ?? new List<KeyValuePair<string, string>>();
            return queryList.Any() ? 
                string.Join("&", queryList.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"))
                : string.Empty;
        }
    }
}
