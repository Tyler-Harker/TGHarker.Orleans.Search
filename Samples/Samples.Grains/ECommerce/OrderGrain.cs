using Orleans.Runtime;
using Samples.Abstractions.ECommerce;

namespace Samples.Grains;

public class OrderGrain : Grain, IOrderGrain
{
    private readonly IPersistentState<OrderState> _state;

    public OrderGrain([PersistentState("order", "Default")] IPersistentState<OrderState> state)
    {
        _state = state;
    }

    public Task<OrderInfo> GetDetailsAsync()
    {
        return Task.FromResult(new OrderInfo(
            _state.State.UserId,
            _state.State.ProductId,
            _state.State.Quantity,
            _state.State.Status,
            _state.State.CreatedAt));
    }

    public async Task CreateOrderAsync(string userId, string productId, int quantity, string status)
    {
        _state.State.UserId = userId;
        _state.State.ProductId = productId;
        _state.State.Quantity = quantity;
        _state.State.Status = status;
        _state.State.CreatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task UpdateStatusAsync(string status)
    {
        _state.State.Status = status;
        await _state.WriteStateAsync();
    }
}
