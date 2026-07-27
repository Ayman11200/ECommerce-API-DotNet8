using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public ICategoryRepository CategoryRepository {  get;  }

        public IProductRepository ProductRepository { get; }


        public UnitOfWork(AppDbContext context)
        {
           _context = context;

            CategoryRepository = new CategoryRepository(context);

            ProductRepository = new ProductRepository(context);
        }
    }
}
