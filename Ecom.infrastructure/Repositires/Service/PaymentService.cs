using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
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


        public Task<CustomerBasket> CreateOrUpdatePaymentAsync(string paymentId, int? deliveryMethodId)
        {

           

        }
    }
}
