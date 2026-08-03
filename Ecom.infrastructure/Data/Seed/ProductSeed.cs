using Ecom.Core.Entities.Product;
using Ecom.infrastructure.Data;

namespace Ecom.infrastructure.Data.Seed;

public static class ProductSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        //if (context.Products.Any())
        //    return;

        var products = new List<Product>
        {
            new Product
            {
                Name = "ASUS ROG Strix G16",
                Description = "Intel Core i7-13650HX, RTX 4060, 16GB RAM, 1TB SSD",
                NewPrice = 68999,
                OldPrice = 73999,
                CategoryId = 3
            },

            new Product
            {
                Name = "Lenovo Legion 5 Pro",
                Description = "Ryzen 7 7840HS, RTX 4070, 16GB RAM, 1TB SSD",
                NewPrice = 79999,
                OldPrice = 85999,
                CategoryId = 3
            },

            new Product
            {
                Name = "Acer Nitro V15",
                Description = "Core i7-13620H, RTX 4050, 16GB RAM, 512GB SSD",
                NewPrice = 52999,
                OldPrice = 57999,
                CategoryId = 3
            },

            new Product
            {
                Name = "HP Omen 16",
                Description = "Ryzen 9, RTX 4070, 32GB RAM, 1TB SSD",
                NewPrice = 94999,
                OldPrice = 99999,
                CategoryId = 3
            },

            new Product
            {
                Name = "MSI Katana 15",
                Description = "Core i7-13620H, RTX 4060, 16GB RAM",
                NewPrice = 63999,
                OldPrice = 68999,
                CategoryId = 3
            },

            new Product
            {
                Name = "Dell G15",
                Description = "Ryzen 7, RTX 3060, 16GB RAM",
                NewPrice = 47999,
                OldPrice = 52999,
                CategoryId = 3
            },

            new Product
            {
                Name = "ASUS TUF A15",
                Description = "Ryzen 7, RTX 4060, 16GB RAM",
                NewPrice = 59999,
                OldPrice = 64999,
                CategoryId = 3
            },

            new Product
            {
                Name = "Gigabyte G5",
                Description = "Core i5, RTX 4050, 16GB RAM",
                NewPrice = 46999,
                OldPrice = 51999,
                CategoryId = 3
            }
        };

        await context.Products.AddRangeAsync(products);

        await context.SaveChangesAsync();
    }
}