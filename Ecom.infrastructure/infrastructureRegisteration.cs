using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using Ecom.infrastructure.Repositires;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ecom.Core.Services;
using Ecom.infrastructure.Repositires.Service;
using Microsoft.Extensions.FileProviders;
using StackExchange.Redis;

namespace Ecom.infrastructure
{
    public static class infrastructureRegisteration
    {
        public static object infrastructureConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(typeof(IGenericRepository<>),typeof(GenericRepository<>));

            //services.AddScoped<ICategoryRepository, CategoryRepository>();

            //services.AddScoped<IProductRepository, ProductRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(configuration.GetConnectionString("redis"));

                return ConnectionMultiplexer.Connect(config);
            });

            services.AddSingleton<IImageManagementService, ImageManagementService>();

            services.AddSingleton<IFileProvider>(
                new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

            services.AddDbContext<AppDbContext>(op =>
            {
                op.UseSqlServer(configuration.GetConnectionString("EcomDatabase"));
            });




            return services;
        }   
    }
}
