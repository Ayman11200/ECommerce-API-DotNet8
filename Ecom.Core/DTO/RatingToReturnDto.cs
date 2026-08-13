using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.DTO
{
    public class RatingToReturnDto
    {
        public string UserName { get; set; }

        public short Stars { get; set; }

        public string? Comment { get; set; }

        public DateTime Review {  get; set; }

    }
    public class AddRatingDto
    {
        public short Stars { get; set; }

        public string? Comment { get; set; }

        public int ProductId { get; set; }

    }
}
