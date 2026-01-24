namespace Samples.Abstractions.ECommerce;

public interface IOrderGrain : IGrainWithStringKey
{
    Task<OrderInfo> GetDetailsAsync();
    Task CreateOrderAsync(string userId, string productId, int quantity, string status);
    Task UpdateStatusAsync(string status);
}

[GenerateSerializer]
public record OrderInfo(
    [property: Id(0)] string UserId,
    [property: Id(1)] string ProductId,
    [property: Id(2)] int Quantity,
    [property: Id(3)] string Status,
    [property: Id(4)] DateTime CreatedAt);
