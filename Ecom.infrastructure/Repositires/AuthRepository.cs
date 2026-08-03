using Ecom.Core.Dto;
using Ecom.Core.Entities;
using Ecom.Core.interfaces;
using Ecom.Core.Services;
using Ecom.Core.Sharing;
using Ecom.infrastructure.Data;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public AuthRepository(UserManager<AppUser> userManager, IEmailService emailService, SignInManager<AppUser> signInManager, IGenerateToken generateToken, AppDbContext context)
        {
            this.userManager = userManager;
            this.emailService = emailService;
            this.signInManager = signInManager;
            this.generateToken = generateToken;
            this.context = context;
        }


        public async Task<string?> RegisterAsync(RegisterDto registerDto)
        {

            if (registerDto is null)
                return null;

            if(await userManager.FindByNameAsync(registerDto.UserName) is not null)
            {
                return "This username is already registerd";
            }

            if (await userManager.FindByEmailAsync(registerDto.Email) is not null)
            {
                return "This Email is already registerd";
            }

            AppUser user = new()
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                DisplayName = registerDto.DisplayName
            };

            var result = await userManager.CreateAsync(user,registerDto.Password);

            if (result.Succeeded is not true)
            {
                return result.Errors.ToList()[0].Description;
            }

            string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendEmail(user.Email, token, "Active", "ActiveEmail", "Please active your email, click on button to active");

            return "done";
        }

        public async Task SendEmail(string email, string code, string component, string subject, string message)
        {
            var result = new EmailDto(email, "almohandis80@gmail.com"
                , subject, EmailStringBody.send(email, code, component, message));

            await emailService.SendEmailAsync(result);    
        }


        public async Task<string?> Login(LoginDto loginDto)
        {
            if (loginDto == null)
                return null;

            var foundUser = await userManager.FindByEmailAsync(loginDto.Email);

            if (foundUser is null)
                return "No user with this Email, Please Register";

            if (!foundUser.EmailConfirmed)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(foundUser);
                await SendEmail(foundUser.Email,token, "Active", "ActiveEmail", "Please active your email, click on button to active");
                return "Please confirem your email first, we have sent activate to your E-mail";
            }

            var result = await signInManager.CheckPasswordSignInAsync(foundUser, loginDto.Password, true);

            if(result.Succeeded)
            {
                return generateToken.GetAndCreateToken(foundUser);
            }

            return "Please check your email or password , something went wrong!";


        }

        public async Task<bool> SendEmailForForgetPassword(string email)
        {
            var foundUser = await userManager.FindByEmailAsync(email); 

            if(foundUser is null)
                return false;

            var token = await userManager.GeneratePasswordResetTokenAsync(foundUser);
            await SendEmail(foundUser.Email, token, "Active", "ActiveEmail", "Please active your email, click on button to active");

            return true;
        }

        public async Task<string?> ResetPassword(PasswordDto passwordDto)
        {
            var foundUser = await userManager.FindByEmailAsync(passwordDto.Email);

            if (foundUser is null)
            {
                return null;
            }

            var result = await userManager.ResetPasswordAsync(foundUser, passwordDto.Token, passwordDto.Password);

            if (result.Succeeded)
            {
                return "done";
            }
            return result.Errors.ToList()[0].Description;
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
                context.Entry(Myaddress).State = EntityState.Detached;
                address.Id = Myaddress.Id;
                address.AppUserId = Myaddress.AppUserId;
                context.Addresses.Update(address);

            }
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Address> getUserAddress(string email)
        {
            var User = await userManager.FindByEmailAsync(email);
            var address = await context.Addresses.FirstOrDefaultAsync(m => m.AppUserId == User.Id);

            return address;
        }

    }
}
