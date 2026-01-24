using Samples.Abstractions.ECommerce;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace Samples.Grains;

[Searchable(typeof(IProductGrain))]
[GenerateSerializer]
public class ProductState
{
    [Queryable]
    [FullTextSearchable(Weight = 2.0)]
    [Id(0)]
    public string Name { get; set; } = string.Empty;

    [Queryable(Indexed = true)]
    [Id(1)]
    public string Category { get; set; } = string.Empty;

    [Queryable]
    [Id(2)]
    public decimal Price { get; set; }

    [Queryable]
    [Id(3)]
    public bool InStock { get; set; }
}
