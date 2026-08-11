using Ecom.Core.Entities;
using Ecom.Core.Entities.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.interfaces
{
    public interface IPaymentService
    {
        Task<CustomerBasket> CreateOrUpdatePaymentAsync(string BasketId, int? deliveryMethodId);
        Task<Order> UpdateOrderSuccess(string PaymentInten);
        Task<Order> UpdateOrderFaild(string PaymentInten);
    }
}
