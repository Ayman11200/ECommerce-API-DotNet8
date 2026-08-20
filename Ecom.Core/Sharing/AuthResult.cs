using Ecom.Core.Dto;
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
        public TokenResult? Tokens { get; set; }

        public static AuthResult Ok(TokenResult tokens , string? message = null)
        {
            return new AuthResult
            {
                Success = true,
                Message = message,
                Tokens = tokens
                
            };
        }

        public static AuthResult Fail(string message)
        {
            return new AuthResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
