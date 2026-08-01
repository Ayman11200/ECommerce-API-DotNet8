using Microsoft.AspNetCore.Http;

public record ProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal NewPrice { get; init; }
    public decimal OldPrice { get; set; }
    public string CategoryName { get; init; }
    public IReadOnlyList<PhotoDto> Photos { get; init; } = [];
}

public record PhotoDto
{
    public string ImageName { get; init; } = string.Empty;

    public int ProductId { get; init; }
}

public record AddProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal NewPrice { get; init; }
    public decimal OldPrice { get; set; }
    public int CategoryId { get; init; }
    public IFormFileCollection Photos { get; init; } 
}

public record UpdateProductDto
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public decimal? NewPrice { get; init; }

    public decimal? OldPrice { get; init; }

    public int? CategoryId { get; init; }

    public IFormFileCollection? Photos { get; init; }
}