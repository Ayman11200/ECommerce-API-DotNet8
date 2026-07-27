using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.DTO
{
  

     public record AddCategoryDto(string Name, string Description);

    public record UpdateCategoryDto(int Id, string Name, string Description);



    
}
