using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Orleans;

/// <summary>
/// Resolves search providers for grain types.
/// </summary>
public interface ISearchProviderResolver
{
    /// <summary>
    /// Gets the search provider for the specified grain type.
    /// Returns the provider as object since the state type is not known at compile time.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use for resolving the provider instance (typically from the request scope).</param>
    object? GetProvider<TGrain>(IServiceProvider serviceProvider) where TGrain : IGrain;
}
