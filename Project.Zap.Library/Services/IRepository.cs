using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> Get(string sql, IDictionary<string, object> parameters = null, string partitionKey = null);

        Task<IEnumerable<T>> Get();

        Task Add(T item);

        Task <T> Update(T item);

        Task Delete(Expression<Func<T, bool>> query);
    }
}
