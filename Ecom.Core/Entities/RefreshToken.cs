using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Entities
{
    public class RefreshToken : BaseEntity<int>
    {

        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; }

        public string AppUserId { get; set; } = null!;

        public AppUser AppUser { get; set; } = null!;
    }
}
