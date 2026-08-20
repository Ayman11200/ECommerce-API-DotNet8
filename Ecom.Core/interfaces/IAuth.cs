using Ecom.Core.Dto;
using Ecom.Core.Entities;
using Ecom.Core.Services;
using Ecom.Core.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.interfaces
{
    public interface IAuth
    {
        Task<AuthResult> RegisterAsync(RegisterDto registerDto);
        Task SendEmail(string email, string code, string component, string subject, string message);

        Task<AuthResult> Login(LoginDto loginDto);

        Task<bool> SendEmailForForgetPassword(string email);

        Task<AuthResult> ResetPassword(PasswordDto passwordDto);

        Task<bool> ActiveAccount(ActiveAccountDto AccountDto);

        Task<bool> UpdateAddress(string email, Address address);

        Task<Address?> getUserAddress(string email);

        Task<AuthResult> RefreshTokenAsync(string refreshToken);

        Task<bool> RevokeRefreshTokenAsync(string refreshToken);

    }
}
