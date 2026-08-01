using AutoMapper;
using Ecom.Core.interfaces;
using Ecom.Core.Services;
using Ecom.infrastructure.Data;
using StackExchange.Redis;
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
        private readonly IConnectionMultiplexer _redis;

        public ICategoryRepository CategoryRepository {  get;  }
        public IProductRepository ProductRepository { get; }

        public ICustomerBasketRepository CustomerBasketRepository {  get; }

        public UnitOfWork(AppDbContext context, IMapper mapper, IImageManagementService imageManagementService , IConnectionMultiplexer redis)
        {
           _context = context;
            _mapper = mapper;
            _imageManagementService = imageManagementService;
            _redis = redis;

            CategoryRepository = new CategoryRepository(_context);

            ProductRepository = new ProductRepository(_context, _mapper, _imageManagementService);

            CustomerBasketRepository = new CustomerBasketRepository(_redis ,_context);
        }




        public async Task<int> SaveChangesAsync()
        {
           return await _context.SaveChangesAsync();
        }
    }
}
