using Ecom.Core.Entities.Order;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.DTO
{
    public record OrderDto
    {
        public string BasketId { get; set; }

        public int DeliveryMethodId { get; set; }

        public ShippingAddressDto ShippingAddressDto { get; set; }
    }

    public record ShippingAddressDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string Street { get; set; }
        public string State { get; set; }
    }

    public record OrderToReturnDTO
    {
        public int Id { get; set; }
        public string BuyerEmail { get; set; }
        public decimal SubTotal { get; set; }
        public ShippingAddress shippingAddress { get; set; }


        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }

        public IReadOnlyList<OrderItemDTO> orderItems { get; set; }
        public string deliveryMethod { get; set; }
        public string status { get; set; }
    }

    public record OrderItemDTO
    {
        public int ProductItemId { get; set; }
        public string MainImage { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }


}
