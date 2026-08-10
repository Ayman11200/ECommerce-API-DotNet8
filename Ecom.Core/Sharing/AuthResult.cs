using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Sharing
{
    public class AuthResult
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public string? Token { get; set; }

        public static AuthResult Ok(string? Message = null, string? Token = null)
          => new() { Success = true, Message = Message, Token = Token };

        public static AuthResult Fail(string Message)
        {
            return new AuthResult()
            {
                Success = false,
                Message = Message
            };
        }

    }
}
