using Azure.Core;
using Ecom.Core.Dto;
using Ecom.Core.Entities;
using Ecom.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Pqc.Crypto.Saber;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires.Service
{
    public class GenerateToken : IGenerateToken
    {
        private readonly IConfiguration configuration;

        public GenerateToken(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public TokenResult GetAndCreateToken(AppUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id),       
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }


            var secret = configuration["Token:Secret"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


            var accessToken = new JwtSecurityToken(
                 issuer: configuration["Token:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
                );

            var accessTokenString =
        new JwtSecurityTokenHandler().WriteToken(accessToken);

            var refreshToken = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64));

            return new TokenResult(
                accessTokenString,
                refreshToken);

        }


    }
}
