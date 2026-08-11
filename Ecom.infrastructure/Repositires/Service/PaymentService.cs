using Ecom.Core.Entities;
using Ecom.Core.Entities.Order;
using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork work;
        private readonly IConfiguration configuration;
        private readonly AppDbContext context;

        public PaymentService(IUnitOfWork work, IConfiguration configuration, AppDbContext context)
        {
            this.work = work;
            this.configuration = configuration;
            this.context = context;
        }


        public async Task<CustomerBasket> CreateOrUpdatePaymentAsync(string basketId, int? deliveryMethodId)
        {
            var basket = await work.CustomerBasketRepository.GetBasketAsync(basketId);

            if (basket == null)
                throw new Exception("Basket not found.");

            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

            decimal shippingPrice = 0m;

            if (deliveryMethodId.HasValue)
            {
                var deliveryMethod = await context.DeliveryMethods.FindAsync(deliveryMethodId.Value);

                shippingPrice = deliveryMethod.Price; 

            }


            PaymentIntentService paymentIntentService = new();
            PaymentIntent _intent;

            if (string.IsNullOrEmpty(basket.PaymentIntentId))
            {
                var option = new PaymentIntentCreateOptions
                {
                    Amount = (long)basket.BasketItems.Sum(m => (m.Price * 100) * m.Quantity) + (long)shippingPrice * 100,
                    Currency = "USD",
                    PaymentMethodTypes = new List<string> { "card" }
                };
                _intent = await paymentIntentService.CreateAsync(option);
                basket.PaymentIntentId = _intent.Id;
                basket.ClientSecret = _intent.ClientSecret;
            }
            else
            {
                var option = new PaymentIntentUpdateOptions
                {
                    Amount = (long)basket.BasketItems.Sum(m => (m.Price * 100) * m.Quantity) + (long)shippingPrice * 100
                };
                await paymentIntentService.UpdateAsync(basket.PaymentIntentId, option); 
            }
            await work.CustomerBasketRepository.UpdateBasketAsync(basket);
            return basket;

        }

        public async Task<Order> UpdateOrderFaild(string PaymentInten)
        {
            var order = await context.Orders.FirstOrDefaultAsync(m => m.PaymentIntentId == PaymentInten);
            if (order is null)
            {
                return null;
            }
            order.Status = Status.PaymentFaild;
            context.Orders.Update(order);
            await context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateOrderSuccess(string PaymentInten)
        {
            var order = await context.Orders.FirstOrDefaultAsync(m => m.PaymentIntentId == PaymentInten);
            if (order is null)
            {
                return null;
            }
            order.Status = Status.PaymentReceived;
            context.Orders.Update(order);
            await context.SaveChangesAsync();
            return order;
        }
    }
}
