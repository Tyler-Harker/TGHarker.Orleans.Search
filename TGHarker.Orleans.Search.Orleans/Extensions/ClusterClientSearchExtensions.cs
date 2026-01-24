using Microsoft.Extensions.DependencyInjection;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;
using TGHarker.Orleans.Search.Core.Query;

namespace TGHarker.Orleans.Search.Orleans.Extensions;

/// <summary>
/// Extension methods for IClusterClient to enable grain search.
/// </summary>
public static class ClusterClientSearchExtensions
{
    /// <summary>
    /// Creates a queryable interface for searching grains by their state.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type to search for.</typeparam>
    /// <param name="client">The Orleans cluster client.</param>
    /// <param name="serviceProvider">The service provider to resolve search providers.</param>
    /// <returns>An IQueryable that can be used to query grains.</returns>
    /// <example>
    /// var users = await client.Search&lt;IUserGrain&gt;(serviceProvider)
    ///     .Where(u => u.Email.Contains("@example.com"))
    ///     .Take(10)
    ///     .ToListAsync();
    /// </example>
    public static IQueryable<TGrain> Search<TGrain>(this IClusterClient client, IServiceProvider serviceProvider)
        where TGrain : IGrain
    {
        // Try to get a provider resolver
        var resolver = serviceProvider.GetService<ISearchProviderResolver>();
        if (resolver != null)
        {
            var provider = resolver.GetProvider<TGrain>(serviceProvider);
            if (provider != null)
            {
                return new OrleansQueryable<TGrain>(provider, client);
            }
        }

        throw new InvalidOperationException(
            $"No search provider registered for grain type {typeof(TGrain).Name}. " +
            $"Ensure the grain's state class is marked with [Queryable] and the search provider is registered.");
    }
}
