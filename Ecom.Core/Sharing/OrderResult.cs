using Ecom.Core.DTO;
using Ecom.Core.Entities.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Sharing
{
    public class OrderResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public OrderToReturnDTO? OrderToReturn { get; set; }

        public static OrderResult Ok(OrderToReturnDTO order, string? Message = null)
          => new() { Success = true, Message = Message ,OrderToReturn = order};

        public static OrderResult Fail(string Message)
        {
            return new OrderResult()
            {
                Success = false,
                Message = Message
            };
        }

    }
}
