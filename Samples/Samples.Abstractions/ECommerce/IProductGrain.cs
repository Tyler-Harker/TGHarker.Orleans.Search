namespace Samples.Abstractions.ECommerce;

public interface IProductGrain : IGrainWithStringKey
{
    Task<ProductInfo> GetDetailsAsync();
    Task SetDetailsAsync(string name, string category, decimal price, bool inStock);
}

[GenerateSerializer]
public record ProductInfo(
    [property: Id(0)] string Name,
    [property: Id(1)] string Category,
    [property: Id(2)] decimal Price,
    [property: Id(3)] bool InStock);
