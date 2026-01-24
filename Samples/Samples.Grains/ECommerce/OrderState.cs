using Samples.Abstractions.ECommerce;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace Samples.Grains;

[Searchable(typeof(IOrderGrain))]
[GenerateSerializer]
public class OrderState
{
    [Queryable]
    [Id(0)]
    public string UserId { get; set; } = string.Empty;

    [Queryable]
    [Id(1)]
    public string ProductId { get; set; } = string.Empty;

    [Queryable]
    [Id(2)]
    public int Quantity { get; set; }

    [Queryable(Indexed = true)]
    [Id(3)]
    public string Status { get; set; } = string.Empty;

    [Queryable]
    [Id(4)]
    public DateTime CreatedAt { get; set; }
}
