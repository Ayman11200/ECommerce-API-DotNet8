using Ecom.Core.Dto;
using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Ecom.Core.Services;
using Ecom.Core.Sharing;
using Ecom.infrastructure.Data;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.Crmf;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ecom.infrastructure.Repositires
{
    public class AuthRepository : IAuth
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IEmailService emailService;
        private readonly SignInManager<AppUser> signInManager;
        private readonly IGenerateToken generateToken;
        private readonly AppDbContext context;
        private readonly IConfiguration configuration;

        public AuthRepository(UserManager<AppUser> userManager, IEmailService emailService, SignInManager<AppUser> signInManager, IGenerateToken generateToken, AppDbContext context, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.emailService = emailService;
            this.signInManager = signInManager;
            this.generateToken = generateToken;
            this.context = context;
            this.configuration = configuration;
        }


        public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
        {

            if (registerDto is null)
                return AuthResult.Fail("Invalid request");

            if (await userManager.FindByNameAsync(registerDto.UserName) is not null)
            {
                return AuthResult.Fail("This username is already registerd");
            }

            if (await userManager.FindByEmailAsync(registerDto.Email) is not null)
            {
                return AuthResult.Fail("This Email is already registerd");
            }

            AppUser user = new()
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                DisplayName = registerDto.DisplayName
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded is not true)
            {
                return AuthResult.Fail(result.Errors.ToList()[0].Description);
            }

            string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendEmail(user.Email, token, "Active", "ActiveEmail", "Please active your email, click on button to active");

            return AuthResult.Ok(null,"done");
        }

        public async Task SendEmail(string email, string code, string component, string subject, string message)
        {
            var result = new EmailDto(email, configuration["EmailSetting:From"]
                , subject, EmailStringBody.send(email, code, component, message));

            await emailService.SendEmailAsync(result);
        }


        public async Task<AuthResult> Login(LoginDto loginDto)
        {
            if (loginDto == null)
                return AuthResult.Fail("Invalid request");

            var foundUser = await userManager.FindByEmailAsync(loginDto.Email);

            if (foundUser is null)
                return AuthResult.Fail("Invalid email or password");


            if (!foundUser.EmailConfirmed)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(foundUser);
                await SendEmail(foundUser.Email, token, "Active", "ActiveEmail", "Please active your email, click on button to active");
                return AuthResult.Fail("Please confirm your email first, we have sent an activation link");
            }

            var result = await signInManager.CheckPasswordSignInAsync(foundUser, loginDto.Password, true);

            if (result.IsLockedOut)
            {
                return AuthResult.Fail("Account locked due to multiple failed attempts, try again later");
            }

            if (result.Succeeded)
            {
                var roles = await userManager.GetRolesAsync(foundUser);

                var tokens = generateToken.GetAndCreateToken(foundUser, roles);

                var refreshToken = new RefreshToken
                {
                    Token = tokens.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    AppUserId = foundUser.Id
                };

                await context.RefreshTokens.AddAsync(refreshToken);
                await context.SaveChangesAsync();

                return AuthResult.Ok(tokens);

            }

            return AuthResult.Fail("Invalid email or password");

        }

        public async Task<bool> SendEmailForForgetPassword(string email)
        {
            var foundUser = await userManager.FindByEmailAsync(email);

            if (foundUser is null)
                return false;

            var token = await userManager.GeneratePasswordResetTokenAsync(foundUser);
            await SendEmail(foundUser.Email, token, "ResetPassword", "Reset Your Password",
           "Click the button below to reset your password");
            return true;
        }

        public async Task<AuthResult> ResetPassword(PasswordDto passwordDto)
        {
            var foundUser = await userManager.FindByEmailAsync(passwordDto.Email);

            if (foundUser is null)
                return AuthResult.Fail("Invalid request");

            var result = await userManager.ResetPasswordAsync(foundUser, passwordDto.Token, passwordDto.Password);

            if (result.Succeeded)
            {
                return AuthResult.Ok(null,"done");
            }
            return AuthResult.Fail(result.Errors.ToList()[0].Description);
        }

        public async Task<bool> ActiveAccount(ActiveAccountDto accountDto)
        {
            var foundUser = await userManager.FindByEmailAsync(accountDto.Email);

            if (foundUser is null)
            {
                return false;
            }

            var result = await userManager.ConfirmEmailAsync(foundUser, accountDto.Token);

            if (result.Succeeded)
            {
                return true;
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(foundUser);

            await SendEmail(foundUser.Email, token, "active", "ActiveEmail", "Please active your email, click on button to active");

            return false;
        }

        public async Task<bool> UpdateAddress(string email, Address address)
        {
            var findUser = await userManager.FindByEmailAsync(email);
            if (findUser is null)
            {
                return false;
            }

            var Myaddress = await context.Addresses.AsNoTracking()
                .FirstOrDefaultAsync(m => m.AppUserId == findUser.Id);

            if (Myaddress is null)
            {
                address.AppUserId = findUser.Id;
                await context.Addresses.AddAsync(address);
            }
            else
            {
                address.Id = Myaddress.Id;
                address.AppUserId = Myaddress.AppUserId;
                context.Addresses.Update(address);

            }
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Address?> getUserAddress(string email)
        {
            var User = await userManager.FindByEmailAsync(email);
            if (User is null) return null;

            return await context.Addresses.FirstOrDefaultAsync(m => m.AppUserId == User.Id);
        }


        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await context.RefreshTokens
                .Include(rt => rt.AppUser)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken is null)
                return AuthResult.Fail("Invalid refresh token.");

            if (storedToken.IsRevoked)
                return AuthResult.Fail("Refresh token has been revoked.");

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
                return AuthResult.Fail("Refresh token has expired.");



            var roles = await userManager.GetRolesAsync(storedToken.AppUser);

            var tokens = generateToken.GetAndCreateToken(
                storedToken.AppUser,
                roles);

            
            storedToken.IsRevoked = true;

            var newRefreshToken = new RefreshToken
            {
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                AppUserId = storedToken.AppUserId
            };

            await context.RefreshTokens.AddAsync(newRefreshToken);

            await context.SaveChangesAsync();

            return AuthResult.Ok(tokens);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (storedToken is null)
                return false;

            storedToken.IsRevoked = true;

            await context.SaveChangesAsync();

            return true;
        }


    }
}
