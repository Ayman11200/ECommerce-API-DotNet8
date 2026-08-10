using Ecom.Core.DTO;
using Ecom.Core.Entities.Order;
using Ecom.Core.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Services
{
    public interface IOrderService
    {

        Task<OrderResult> CreateOrderAsync(OrderDto orderDto, string email);

        Task<IReadOnlyCollection<OrderToReturnDTO>> GetAllOrdersForUserAsync(string email);

        Task<OrderToReturnDTO> GetOrderByIdAsync(int Id , string email);

        Task<IReadOnlyCollection<DeliveryMethod>> GetDeliveryMethodsAsync();

    }
}
