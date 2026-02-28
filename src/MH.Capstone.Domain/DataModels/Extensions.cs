using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;

namespace MH.Capstone.Domain.DataModels
{
    public static class Extensions
    {
        public static async Task SaveModelAsync<TModel>(this TModel model,
            IRepository<TModel, ApplicationDbContext> repo) where TModel : class, new()
                => await repo.AddOrUpdateAsync(model);
    }
}
