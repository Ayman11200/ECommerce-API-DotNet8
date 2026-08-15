using Ecom.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Services
{
    public interface IGenerateToken
    {

        public string GetAndCreateToken(AppUser user , IList<string> roles);

    }
}
