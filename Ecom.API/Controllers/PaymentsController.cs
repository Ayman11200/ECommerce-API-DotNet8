using Ecom.Core.Entities;
using Ecom.Core.Entities.Order;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Ecom.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
  
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            this.paymentService = paymentService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CustomerBasket>> Create(string basketId, int? deliveryMethodId)
        {
            return await paymentService.CreateOrUpdatePaymentAsync(basketId, deliveryMethodId);
        }


        const string endpointSecret = "whsec_YOUR_STRIPE_WEBHOOK_SECRET";

        [HttpPost("webhook")]
        public async Task<IActionResult> UpdateStatusWithStripe()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], endpointSecret, throwOnApiVersionMismatch: false);
                PaymentIntent intent;
                Order orders;
           
                if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
                {
                    intent = stripeEvent.Data.Object as PaymentIntent;
                    orders = await paymentService.UpdateOrderFaild(intent.Id);
                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    intent = stripeEvent.Data.Object as PaymentIntent;
                    orders = await paymentService.UpdateOrderSuccess(intent.Id);
                }
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest();
            }
        }

    }
}
