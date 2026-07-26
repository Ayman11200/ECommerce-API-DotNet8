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
        public ICategoryRepositry CategoryRepositry {  get;  }

        public IProductRepositry ProductRepositry { get; }


        public UnitOfWork(AppDbContext context)
        {
           _context = context;

            CategoryRepositry = new CategoryRepositry(context);

            ProductRepositry = new ProductRepositry(context);
        }
    }
}
