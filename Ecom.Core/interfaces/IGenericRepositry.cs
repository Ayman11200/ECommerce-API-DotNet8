using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.interfaces
{
    public interface IGenericRepository<T> where T : class
    {

        Task<IReadOnlyList<T>> GetAllAsync();

        Task<IReadOnlyList<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        Task<T?> GetByIdAsync(int Id);

        Task<T?> GetByIdAsync(int Id, params Expression<Func<T, object>>[] includes);

        void Update(T entity);

        Task<bool> DeleteAsync(int Id);

        Task AddAsync(T entity);

    }
}
