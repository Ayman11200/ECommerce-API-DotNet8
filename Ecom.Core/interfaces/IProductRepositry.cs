using Ecom.Core.Entities.Product;
using Ecom.Core.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.interfaces
{
    public interface IProductRepository : IGenericRepository<Product> 
    {

        public Task<IEnumerable<ProductDto>> GetAllAsync(ProductParams productParams);

        public Task DeleteAsync(Product product);

        public Task<bool> UpdateAsync(UpdateProductDto updateProductDto);

        public Task<Product> AddAsync(AddProductDto addProductDto);

    }
}
