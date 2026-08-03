using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.Dto;
using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;

namespace Ecom.API.Controllers
{

    public class BasketController : BaseController
    {
        public BasketController(IUnitOfWork work, IMapper mapper) : base(work, mapper)
        {


        }

        [HttpGet("{Id}")]
        [ProducesResponseType(typeof(CustomerBasket), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseAPI), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(string Id)
        {
            var result = await work.CustomerBasketRepository.GetBasketAsync(Id);

            return Ok(result ?? new CustomerBasket(Id));
                
        }

        [HttpPut]
        [ProducesResponseType(typeof(CustomerBasket), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(CustomerBasket basket)
        {

            var result = await work.CustomerBasketRepository.UpdateBasketAsync(basket);

            return result is null? BadRequest(new ResponseAPI(400, "Invalid basket or products.")) : Ok(result);
        }


        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(string Id)
        {
            var deleted = await work.CustomerBasketRepository.DeleteBasketAsync(Id);

            return deleted ? Ok(new ResponseAPI(200, "item deleted!")) :
                NotFound(new ResponseAPI(404, $"Basket with Id = {Id} not found."));
        }


    }
}
