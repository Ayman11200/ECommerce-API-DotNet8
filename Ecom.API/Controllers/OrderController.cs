using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.DTO;
using Ecom.Core.Entities.Order;
using Ecom.Core.Services;
using Ecom.Core.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecom.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService orderService;

        public OrderController(IOrderService orderService)
        {
            this.orderService = orderService;
        }

        [HttpPost("Create-order")]
        [ProducesResponseType(typeof(OrderToReturnDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(OrderDto orderDto)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            var result = await orderService.CreateOrderAsync(orderDto, email);

            if (!result.Success)
                return BadRequest(new ResponseAPI(400, result.Message));

            return Ok(result.OrderToReturn);       
        }

        [HttpGet("Get-Orders-for-User")]
        [ProducesResponseType(typeof(IReadOnlyCollection<OrderToReturnDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> getorders()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var result = await orderService.GetAllOrdersForUserAsync(email);

            return Ok(result);
        }


        [HttpGet("Get-order-by-id/{id}")]
        [ProducesResponseType(typeof(OrderToReturnDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderById(int Id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            var result = await orderService.GetOrderByIdAsync(Id , email);

            if(result == null) return NotFound(new ResponseAPI(404,
                $"Order with Id {Id} not found!"));

            return Ok(result);
        }


        [HttpGet("Get-delivery")]
        [ProducesResponseType(typeof(IReadOnlyCollection<DeliveryMethod>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeliveryMethods()
        {
            return Ok(await orderService.GetDeliveryMethodsAsync());
        }
    }
}
