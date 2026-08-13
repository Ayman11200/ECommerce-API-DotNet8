using Ecom.Core.DTO;
using Ecom.Core.Entities;
using Ecom.Core.Entities.Product;
using Ecom.Core.interfaces;
using Ecom.infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires
{
    public class RatingReposiroty : IRatingRepository
    {
        private readonly AppDbContext context;
        private readonly UserManager<AppUser> userManager;

        public RatingReposiroty(AppDbContext context, UserManager<AppUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<bool> AddRatingAsync(AddRatingDto addRatingDto,string UserId)
        {
            var user = await userManager.FindByIdAsync(UserId);

            if (await context.Ratings.AsNoTracking().AnyAsync(m => m.ProductId == addRatingDto.ProductId && m.AppUser.Id == UserId))
            {
                return false;
            }      

            var rating = new Rating
            {
                Stars = addRatingDto.Stars,
                Comment = addRatingDto.Comment,
                AppUserId = UserId,
                ProductId = addRatingDto.ProductId,
            };
            await context.Ratings.AddAsync(rating);
            await context.SaveChangesAsync();

            var product = await context.Products.FindAsync(addRatingDto.ProductId);

            var ratings = await context.Ratings.Where(m => m.ProductId == product.Id).ToListAsync();

            if (ratings.Count > 0)
            {
                var average = ratings.Average(m => m.Stars);

                var roundRating = Math.Round(average * 2, MidpointRounding.AwayFromZero) / 2;

                product.Rating = roundRating;
            }
            else
            {
                product.Rating = addRatingDto.Stars;
            }
            await context.SaveChangesAsync();
            return true;

        }

        public async Task<IReadOnlyCollection<RatingToReturnDto>> GetAllRatingForProduct(int ProductId)
        {
            return await context.Ratings
                 .Where(r => r.ProductId == ProductId)
                 .Select(r => new RatingToReturnDto
                 {
                     Stars = r.Stars,
                     Comment = r.Comment,
                     UserName = r.AppUser.UserName,
                     Review = r.Review
                 })
                 .AsNoTracking()
                 .ToListAsync();
        }

    }
}
