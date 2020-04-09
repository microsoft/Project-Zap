using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Project.Zap.Library.Services
{
    public interface IRepository<T>
    {
        T Get(string id);

        IEnumerable<T> Get(Expression<Func<T, bool>> query);

        Task<IEnumerable<T>> Get();

        Task Add(T item);

        Task <T> Update(T item);

        Task Delete(Expression<Func<T, bool>> query);
    }
}
