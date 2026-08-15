using AutoMapper;
using Ecom.Core;
using Ecom.Core.DTO;
using Ecom.Core.Entities.Order;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Ecom.Core.Services;
using Ecom.Core.Sharing;
using Ecom.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Org.BouncyCastle.Crypto;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StackExchange.Redis.Role;

namespace Ecom.infrastructure.Repositires.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork work;
        private readonly AppDbContext context;
        private readonly IMapper mapper;
        private readonly IPaymentService paymentService;



        public OrderService(IUnitOfWork work, AppDbContext context, IMapper mapper, IPaymentService paymentService)
        {
            this.work = work;
            this.context = context;
            this.mapper = mapper;
            this.paymentService = paymentService;
        }

        public async Task<OrderResult> CreateOrderAsync(OrderDto orderDto, string email)
        {
            if (orderDto == null)
                return OrderResult.Fail("Order data is required.");


            var basket = await work.CustomerBasketRepository.GetBasketAsync(orderDto.BasketId);
            if (basket == null || !basket.BasketItems.Any())
                return OrderResult.Fail("Basket is empty or does not exist.");

            var deliveryMethod = await context.DeliveryMethods.FindAsync(orderDto.DeliveryMethodId);
            if (deliveryMethod == null)
                return OrderResult.Fail("Selected delivery method is invalid.");

            List<OrderItem> orderItems = new List<OrderItem>();

            var ids = basket.BasketItems.Select(x => x.ProductId).ToList();

            var products = await context.Products
               .Where(p => ids.Contains(p.Id))
               .ToDictionaryAsync(p => p.Id);

            foreach (var item in basket.BasketItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    return OrderResult.Fail($"Product {item.ProductId} is no longer available.");

                var orderItem = new OrderItem
                {
                    ProductItemId = product.Id,
                    ProductName = product.Name,
                    Price = item.Price,
                    MainImage = item.Image,
                    Quantity = item.Quantity
                };

                orderItems.Add(orderItem);
            }

            var subTotal = orderItems.Sum(oi => oi.Price * oi.Quantity);

            var shippingAddress = mapper.Map<ShippingAddress>(orderDto.ShippingAddressDto);

            var ExsistOrder = await context.Orders
                .Where(o => o.PaymentIntentId == basket.PaymentIntentId)
                .FirstOrDefaultAsync();

            if (ExsistOrder is not null)
            {
                context.Orders.Remove(ExsistOrder);
                await context.SaveChangesAsync();

                await paymentService.CreateOrUpdatePaymentAsync(basket.Id, deliveryMethod.Id);
            }

            var order = new Order(email, subTotal, deliveryMethod, orderItems, shippingAddress, basket.PaymentIntentId);

            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();
            await work.CustomerBasketRepository.DeleteBasketAsync(orderDto.BasketId);

            var orderToReturn = mapper.Map<OrderToReturnDTO>(order);

            return OrderResult.Ok(orderToReturn);

        }


        public async Task<IReadOnlyCollection<OrderToReturnDTO>> GetAllOrdersForUserAsync(string email)
        {

            var orders = await context.Orders.Where(o => o.BuyerEmail == email)
                .Include(o => o.OrderItems)
                .Include(o => o.DeliveryMethod)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate).ToListAsync();

            var OrderDtos = mapper.Map<IReadOnlyCollection<OrderToReturnDTO>>(orders);

            return OrderDtos;

        }


        public async Task<OrderToReturnDTO> GetOrderByIdAsync(int Id, string email)
        {
            var order = await context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.DeliveryMethod)
                .Where(o => o.Id == Id && o.BuyerEmail == email)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var result = mapper.Map<OrderToReturnDTO>(order);

            return result;



     

        }

        public async Task<IReadOnlyCollection<DeliveryMethod>> GetDeliveryMethodsAsync()
            => await context.DeliveryMethods.AsNoTracking().ToListAsync();






    }
}
