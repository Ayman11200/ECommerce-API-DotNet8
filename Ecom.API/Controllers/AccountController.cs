using AutoMapper;
using Ecom.API.Helper;
using Ecom.Core.Dto;
using Ecom.Core.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


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

            Response.Cookies.Append("token", result.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                Domain = configuration["CookieSettings:Domain"],
                Expires = DateTime.Now.AddMinutes(60)
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




        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("token");
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


    }

}