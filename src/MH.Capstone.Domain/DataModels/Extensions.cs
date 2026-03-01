using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ExpressionBuilder.Generics;
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
