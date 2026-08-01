using Ecom.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.interfaces
{
    public interface ICustomerBasketRepository
    {
        public Task<CustomerBasket?> GetBasketAsync(string Id);

        public Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket);

        public Task<bool> DeleteBasketAsync(string Id);


    }
}
