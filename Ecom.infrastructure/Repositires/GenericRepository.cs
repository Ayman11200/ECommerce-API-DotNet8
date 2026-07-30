using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {

        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }


        public async Task AddAsync(T entity)
        {

            await _context.Set<T>().AddAsync(entity);

        }

        public async Task<int> CountAsync()
        => await _context.Set<T>().CountAsync();
        

        public async Task<bool> DeleteAsync(int Id)
        {
            var entity = await _context.Set<T>().FindAsync(Id);

            if (entity == null) return false;

            _context.Set<T>().Remove(entity);
            return true;

        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
         => await _context.Set<T>().AsNoTracking().ToListAsync();
        

        public async Task<IReadOnlyList<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            var query = _context.Set<T>().AsQueryable();

            foreach(var item in includes)
            {
               query = query.Include(item);    
            }
            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<T> GetByIdAsync(int Id)
        {
            var entity = await _context.Set<T>().FindAsync(Id);
            return entity;
        }

        public async Task<T> GetByIdAsync(int Id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            foreach (var include in includes)
            {
               query = query.Include(include);
            }

            var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => EF.Property<int>(x, "Id") == Id);

            return entity;
        }

        public void Update(T entity)
        { 
            var entry = _context.Entry(entity);

            if (entry.State == EntityState.Detached)
                _context.Set<T>().Attach(entity);

            entry.State = EntityState.Modified;

        }
    }
}
