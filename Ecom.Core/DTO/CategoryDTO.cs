using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Ecom.Core.Dto
{

    public record AddCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; init; } = default!;

        [MaxLength(500)]
        public string? Description { get; init; }
    }

    public record CategoryDto
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; init; } = default!;

        [MaxLength(500)]
        public string? Description { get; init; }
    }

}
