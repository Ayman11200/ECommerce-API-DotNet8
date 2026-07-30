using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Sharing
{
    public class ProductParams
    {
        private const int _DefaultPageSize = 3;

        private const int _MaxAllowedPageSize = 6;

        public string? Sort { get; set; }

        public int? CategoryId { get; set; }

        public string? Search { get; set; }

        public int PageNumber { get; set; } = 1;

        public int _PageSize = _DefaultPageSize;

        public int PageSize
        {
            get {  return _PageSize; }
            set { _PageSize = value > _MaxAllowedPageSize ? _MaxAllowedPageSize : value; }
        }

        public int TotalCount { get; set; }


    }
}
