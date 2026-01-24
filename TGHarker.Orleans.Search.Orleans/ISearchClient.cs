using System;
using Orleans;

namespace TGHarker.Orleans.Search.Orleans;

/// <summary>
/// Client for searching Orleans grains by their state.
/// </summary>
/// <remarks>
/// This interface is obsolete. Use <see cref="IClusterClient"/> with the Search extension methods instead.
/// After calling <c>AddOrleansSearch()</c>, the injected <see cref="IClusterClient"/> will support search operations.
/// </remarks>
[Obsolete("Use IClusterClient.Search<TGrain>() instead. Inject IClusterClient and use the generated Search extension methods. This interface will be removed in a future version.")]
public interface ISearchClient
{
    /// <summary>
    /// Creates a queryable interface for searching grains by their state.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type to search for.</typeparam>
    /// <returns>An IQueryable that can be used to query grains.</returns>
    /// <example>
    /// var users = await searchClient.Search&lt;IUserGrain&gt;()
    ///     .Where(u => u.Email.Contains("@example.com"))
    ///     .Take(10)
    ///     .ToListAsync();
    /// </example>
    IQueryable<TGrain> Search<TGrain>() where TGrain : IGrain;
}
