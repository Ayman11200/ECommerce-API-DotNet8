using Ecom.Core.Dto;
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
        Task<string?> RegisterAsync(RegisterDto registerDto);
        Task SendEmail(string email, string code, string component, string subject, string message);

        Task<string?> Login(LoginDto loginDto);

        Task<bool> SendEmailForForgetPassword(string email);

        Task<string?> ResetPassword(PasswordDto passwordDto);

        Task<bool> ActiveAccount(ActiveAccountDto AccountDto);



    }
}
