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

            await _context.SaveChangesAsync();

        }

        public async Task Delete(int Id)
        {
            var entity = await _context.Set<T>().FindAsync(Id);
            
            // Add Code To Check if Entity Exists

            _context.Set<T>().Remove(entity);

            await _context.SaveChangesAsync();
            // You Shouldnt put SaveChanges Here while using Unit Of Work
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

        public async Task<T> GetById(int Id)
        {
            var entity = await _context.Set<T>().FindAsync(Id);
            return entity;
        }

        public async Task<T> GetById(int Id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            foreach (var item in includes)
            {
               query = query.Include(item);
            }

            var entity = await query.FirstOrDefaultAsync(x => EF.Property<int>(x, "Id") == Id);

            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            //Change it to Load Then Update 
        }
    }
}
