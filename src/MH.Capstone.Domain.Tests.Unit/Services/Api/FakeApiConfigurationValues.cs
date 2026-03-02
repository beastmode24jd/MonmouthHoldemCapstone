using MH.Capstone.Domain.ApiContracts;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Tests.Unit.Services.Api
{
    internal class ApiConfigurationValuesFake : ApiConfigurationValues
    {
        public ApiConfigurationValuesFake() : 
            base("test", "http://www.example.com/api/vt", [])
        { }

        public static ApiConfigurationValuesFake Instance = new();
    }
}
