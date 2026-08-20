using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Entities
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; }

        public Address? Address { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; }
          = new List<RefreshToken>();

    }
}
