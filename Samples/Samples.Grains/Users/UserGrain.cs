using Orleans.Runtime;
using Samples.Abstractions.Users;

namespace Samples.Grains;

public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<UserState> _state;

    public UserGrain([PersistentState("user", "Default")] IPersistentState<UserState> state)
    {
        _state = state;
    }

    public Task<UserInfo> GetInfoAsync()
    {
        return Task.FromResult(new UserInfo(
            _state.State.Email,
            _state.State.DisplayName,
            _state.State.IsActive));
    }

    public async Task SetInfoAsync(string email, string displayName, bool isActive)
    {
        _state.State.Email = email;
        _state.State.DisplayName = displayName;
        _state.State.IsActive = isActive;
        await _state.WriteStateAsync();
    }
}
