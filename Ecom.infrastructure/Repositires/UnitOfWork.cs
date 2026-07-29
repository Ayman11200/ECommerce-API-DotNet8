using AutoMapper;
using Ecom.Core.interfaces;
using Ecom.Core.Services;
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
        private readonly IMapper _mapper;
        private readonly IImageManagementService _imageManagementService;

        public ICategoryRepository CategoryRepository {  get;  }
        public IProductRepository ProductRepository { get; }


        public UnitOfWork(AppDbContext context, IMapper mapper, IImageManagementService imageManagementService)
        {
           _context = context;
            _mapper = mapper;
            _imageManagementService = imageManagementService;

            CategoryRepository = new CategoryRepository(_context);


            ProductRepository = new ProductRepository(_context, _mapper, _imageManagementService);
        }




        public async Task<int> SaveChangesAsync()
        {
           return await _context.SaveChangesAsync();
        }
    }
}
