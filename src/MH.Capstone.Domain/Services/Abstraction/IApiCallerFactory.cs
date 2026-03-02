using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.ApiContracts;
using Microsoft.Extensions.DependencyInjection;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IApiCallerFactory<out TConfig> where TConfig : class, IApiConfigurationValues
    {
        IApiCaller<TConfig> CreateApiCaller();
    }
}