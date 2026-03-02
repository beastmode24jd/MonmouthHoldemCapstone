using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IApiCaller
    {
        Task<T> GetAsync<T>(string url);
    }
}
