using Ecom.Core.DTO;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecom.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingRepository rating;

        public RatingsController(IRatingRepository rating)
        {
            this.rating = rating;
        }


        [HttpGet("get-rating/{productId}")]
        public async Task<IActionResult> get(int productId)
        {
            var result = await rating.GetAllRatingForProduct(productId);
            return Ok(result);
        }


        [HttpPost("add-rating")]
        public async Task<IActionResult> add(AddRatingDto ratings)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var result = await rating.AddRatingAsync(ratings, email);
            return result ? Ok() : BadRequest();
        }
    }
}
