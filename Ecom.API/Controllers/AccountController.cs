using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.Dto;
using Ecom.Core.DTO;
using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace Ecom.API.Controllers
{

    public class AccountController : BaseController
    {
        private readonly IConfiguration configuration;
        public AccountController(IUnitOfWork work, IMapper mapper ,IConfiguration configuration) : base(work, mapper)
        {
            this.configuration = configuration;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var result = await work.Auth.RegisterAsync(registerDto);

            if (!result.Success)
            {
                return BadRequest(new ResponseAPI(400, result.Message));
            }
            return Ok();
        }


        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var result = await work.Auth.Login(loginDto);

            if (!result.Success)
            {
                return BadRequest(new ResponseAPI(400, result.Message));
            }

            Response.Cookies.Append("token", result.Tokens!.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Domain = configuration["CookieSettings:Domain"],
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", result.Tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Domain = configuration["CookieSettings:Domain"],
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new ResponseAPI(200));

        }


        [HttpPost("Active-Account")]
        public async Task<ActionResult<ActiveAccountDto>> Active(ActiveAccountDto AccountDto)
        {
            var result = await work.Auth.ActiveAccount(AccountDto);
            return result ? Ok(new ResponseAPI(200)) : BadRequest(new ResponseAPI(400));
        }

        [HttpPost("Send-email-forget-password")]
        public async Task<IActionResult> Forget(string email)
        {
            var result = await work.Auth.SendEmailForForgetPassword(email);

            return result ? Ok(new ResponseAPI(200)) : BadRequest(new ResponseAPI(400));
        }

        [HttpPost("Reset-password")]
        public async Task<IActionResult> Reset(PasswordDto restPasswordDTO)
        {
            var result = await work.Auth.ResetPassword(restPasswordDTO);
            if (result.Success)
            {
                return Ok(new ResponseAPI(200));
            }
            return BadRequest(new ResponseAPI(400, result.Message));
        }



        [HttpPost("Refresh-Token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new ResponseAPI(401, "Refresh token is missing."));

            var result = await work.Auth.RefreshTokenAsync(refreshToken);

            if (!result.Success)
                return Unauthorized(new ResponseAPI(401, result.Message));

            Response.Cookies.Append("token", result.Tokens!.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Domain = configuration["CookieSettings:Domain"],
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", result.Tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Domain = configuration["CookieSettings:Domain"],
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new ResponseAPI(200));
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await work.Auth.RevokeRefreshTokenAsync(refreshToken);
            }

            Response.Cookies.Delete("token");
            Response.Cookies.Delete("refreshToken");

            return Ok(new ResponseAPI(200));
        }

        [Authorize]
        [HttpGet("Get-user-name")]
        public IActionResult GetUserName()
        {
            return Ok(new ResponseAPI(200, User.Identity.Name));
        }

        [HttpGet("IsUserAuth")]
        public async Task<IActionResult> IsUserAuth()
        {
            return User.Identity.IsAuthenticated ? Ok() : BadRequest();
        }




        [Authorize]
        [HttpPut("update-address")]
        public async Task<IActionResult> updateAddress(ShippingAddressDto addressDTO)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var address = mapper.Map<Address>(addressDTO);
            var result = await work.Auth.UpdateAddress(email,address);
            return result ? Ok() : BadRequest();
        }

        [HttpGet("get-address-for-user")]
        public async Task<IActionResult> getAddress()
        {
            var address = await work.Auth.getUserAddress(User.FindFirst(ClaimTypes.Email).Value);
            var result = mapper.Map<ShippingAddressDto>(address);
            return Ok(result);
        }


    }

}