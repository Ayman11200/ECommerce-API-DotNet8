using Ecom.Core.Entities.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecom.Core.DTO;

namespace Ecom.Core.interfaces
{
    public interface IRatingRepository
    {
        Task<IReadOnlyCollection<RatingToReturnDto>> GetAllRatingForProduct(int ProductId);

        Task<bool> AddRatingAsync(AddRatingDto addRatingDto, string UserId);

    }
}
