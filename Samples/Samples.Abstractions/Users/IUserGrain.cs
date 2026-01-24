namespace Samples.Abstractions.Users;

public interface IUserGrain : IGrainWithStringKey
{
    Task<UserInfo> GetInfoAsync();
    Task SetInfoAsync(string email, string displayName, bool isActive);
}

[GenerateSerializer]
public record UserInfo(
    [property: Id(0)] string Email,
    [property: Id(1)] string DisplayName,
    [property: Id(2)] bool IsActive);
