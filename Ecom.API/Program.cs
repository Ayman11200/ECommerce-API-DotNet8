using AutoMapper;
using Ecom.API.Authorization;
using Ecom.API.Extensions;
using Ecom.infrastructure;
using Ecom.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Ecom.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

      
            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                    policy.WithOrigins("https://your-frontend-domain.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()); 
            });
           
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.infrastructureConfiguration(builder.Configuration);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


           
            builder.Services.AddSingleton<IAuthorizationHandler, BasketOwnerOrAdminHandler>();
            builder.Services.AddSingleton<IAuthorizationHandler, OrderOwnerOrAdminHandler>();


            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("BasketOwnerOrAdmin", policy =>
                    policy.Requirements.Add(new BasketOwnerOrAdminRequirement()));

                options.AddPolicy("OrderOwnerOrAdmin", policy =>
                    policy.Requirements.Add(new OrderOwnerOrAdminRequirement()));
            });


            var app = builder.Build();

            await app.SeedRolesAsync();
            await app.SeedAdminUserAsync();

            app.UseCustomExceptionMiddleware();
            app.UseAuthentication();
            app.UseAuthorization();

            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseCors("AllowFrontend");

            app.MapControllers();
           
            app.Run();
        }
    }
}
