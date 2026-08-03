using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ecom.Core.Dto
{


    public record LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public record RegisterDto : LoginDto
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }

    }
    public record PasswordDto : LoginDto
    {
        public string Token { get; set; }
    }

    public record ActiveAccountDto
    {
        public string Email { get; set; }

        public string Token { get; set; }
    }

}
