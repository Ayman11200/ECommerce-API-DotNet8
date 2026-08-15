using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires
{
    public class CustomerBasketRepository : ICustomerBasketRepository
    {
        private readonly IDatabase _database;
        private readonly AppDbContext _context;
        public CustomerBasketRepository(IConnectionMultiplexer redis , AppDbContext context)
        {
            _database = redis.GetDatabase();
            _context = context;
        }



        public async Task<bool> DeleteBasketAsync(string Id)
        {
            return await _database.KeyDeleteAsync(Id);
        }

        public async Task<CustomerBasket?> GetBasketAsync(string Id)
        {
            var result = await _database.StringGetAsync(Id);

            if (!string.IsNullOrEmpty(result))
            {
                return JsonSerializer.Deserialize<CustomerBasket>(result);
            }
            
            return null;
        }

        public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket)
        {
            var ids = basket.BasketItems.Select(x => x.Id).ToList();

            var products = await _context.Products
                 .Include(m => m.Photos)
                 .Include(m => m.Category)
                 .Where(p => ids.Contains(p.Id))
                 .ToDictionaryAsync(p => p.Id);

            if (ids.Count() != products.Count())
                return null;

            foreach (var item in basket.BasketItems)
            {
                var product = products[item.Id];

                item.ProductId = product.Id;
                item.Name = product.Name;
                item.Price = product.NewPrice;
                item.Description = product.Description;
                item.CategoryName = product.Category.Name;
                item.Image = product.Photos.FirstOrDefault()?.ImageName ?? "";
            }

            var saved = await _database.StringSetAsync(basket.Id, JsonSerializer.Serialize(basket), TimeSpan.FromDays(3));
                     
            return saved ? basket : null;

        }

    }
}
