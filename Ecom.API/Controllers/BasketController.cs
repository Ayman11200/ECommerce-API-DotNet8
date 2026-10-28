using AutoMapper;
using Ecom.API.Extensions;
using Ecom.API.Helper;
using Ecom.Core.Dto;
using Ecom.Core.DTO;
using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;

namespace Ecom.API.Controllers
{

    public class BasketController : BaseController
    {
        private readonly IAuthorizationService authorizationService;
        public BasketController(IUnitOfWork work, IMapper mapper, IAuthorizationService authorizationService) : base(work, mapper)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet("{Id}")]
        [ProducesResponseType(typeof(CustomerBasket), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get(string Id)
        {
            var basket = await work.CustomerBasketRepository.GetBasketAsync(Id);

            if (basket == null)
                return Ok(new BasketDto { Id = Id ,BasketItems = new List<BasketItem>() });

            var authResult = await authorizationService.AuthorizeAsync(User, basket, "BasketOwnerOrAdmin");
            if (!authResult.Succeeded)
                return Forbid();

            var resultDto = mapper.Map<BasketDto>(source: basket);


            return Ok(resultDto);
                
        }

        [HttpPut]
        [ProducesResponseType(typeof(CustomerBasket), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(BasketDto dto)
        {
            var existing = await work.CustomerBasketRepository.GetBasketAsync(dto.Id);

            if (existing is not null)
            {
                var authResult = await authorizationService.AuthorizeAsync(User, existing, "BasketOwnerOrAdmin");
                if (!authResult.Succeeded)
                    return Forbid();
            }

            var basket = mapper.Map<CustomerBasket>(dto);
            basket.OwnerId = existing?.OwnerId ?? User.GetUserId();
            basket.PaymentIntentId = existing?.PaymentIntentId;
            basket.ClientSecret = existing?.ClientSecret;

            var result = await work.CustomerBasketRepository.UpdateBasketAsync(basket);
            if (result is null)
                return BadRequest(new ResponseAPI(400, "Invalid basket or products."));

            return Ok(mapper.Map<BasketDto>(result));
        }


        [HttpDelete("{Id}")]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(string Id)
        {
            var existing = await work.CustomerBasketRepository.GetBasketAsync(Id);
            if (existing is null)
                return NotFound(new ResponseAPI(404, $"Basket with Id = {Id} not found."));

            var authResutl = await authorizationService.AuthorizeAsync(User, existing, "BasketOwnerOrAdmin");
            if (!authResutl.Succeeded)
                return Forbid();

            var deleted = await work.CustomerBasketRepository.DeleteBasketAsync(Id);

            return deleted
                ? Ok(new ResponseAPI(200, "item deleted!"))
                : StatusCode(500, new ResponseAPI(500, "Failed to delete basket."));
        }


    }
}
