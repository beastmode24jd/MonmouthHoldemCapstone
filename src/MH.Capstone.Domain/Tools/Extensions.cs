using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
    }
}
