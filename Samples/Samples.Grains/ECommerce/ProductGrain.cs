using Orleans.Runtime;
using Samples.Abstractions.ECommerce;

namespace Samples.Grains;

public class ProductGrain : Grain, IProductGrain
{
    private readonly IPersistentState<ProductState> _state;

    public ProductGrain([PersistentState("product", "Default")] IPersistentState<ProductState> state)
    {
        _state = state;
    }

    public Task<ProductInfo> GetDetailsAsync()
    {
        return Task.FromResult(new ProductInfo(
            _state.State.Name,
            _state.State.Category,
            _state.State.Price,
            _state.State.InStock));
    }

    public async Task SetDetailsAsync(string name, string category, decimal price, bool inStock)
    {
        _state.State.Name = name;
        _state.State.Category = category;
        _state.State.Price = price;
        _state.State.InStock = inStock;
        await _state.WriteStateAsync();
    }
}
