using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Entities.Order
{
    public class Order : BaseEntity<int>
    {
        public Order()
        {
            
        }
        public Order(string buyerEmail, decimal subTotal, DeliveryMethod deliveryMethod, 
            List<OrderItem> orderItems, ShippingAddress shippingAddress, string paymentIntentId)
        {
            BuyerEmail = buyerEmail;
            SubTotal = subTotal;
            DeliveryMethod = deliveryMethod;
            OrderItems = orderItems;
            ShippingAddress = shippingAddress;
            PaymentIntentId = paymentIntentId ;
            Total = SubTotal + deliveryMethod.Price;
        }

        [Required]
        [MaxLength(256)]
        public string BuyerEmail { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public Status Status { get; set; } = Status.Pending;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Required]
        public int DeliveryMethodId { get; set; }

        public string PaymentIntentId { get; set; }


        public DeliveryMethod DeliveryMethod { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public ShippingAddress ShippingAddress { get; set; }

        public decimal Total { get; private set; }
           


    }
}
