using Orleans;

namespace TGHarker.Orleans.Search.Orleans;

/// <summary>
/// Extension of IClusterClient that provides search capabilities for Orleans grains.
/// </summary>
/// <remarks>
/// This interface is implemented by <see cref="SearchableClusterClient"/> which decorates
/// the Orleans IClusterClient with search functionality. Register via <c>AddOrleansSearch()</c>
/// and inject <see cref="IClusterClient"/> - it will automatically resolve to the searchable version.
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddOrleansSearch();
///
/// // In controller - just inject IClusterClient
/// public UserController(IClusterClient clusterClient)
/// {
///     var users = await clusterClient
///         .Search&lt;IUserGrain&gt;(u => u.Email.Contains("@example.com"))
///         .ToListAsync();
/// }
/// </code>
/// </example>
public interface ISearchableClusterClient : IClusterClient
{
    /// <summary>
    /// Creates a queryable interface for searching grains by their state.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type to search for.</typeparam>
    /// <returns>An IQueryable that can be used to query grains.</returns>
    /// <example>
    /// <code>
    /// var users = await clusterClient.Search&lt;IUserGrain&gt;()
    ///     .Where(u => u.Email.Contains("@example.com"))
    ///     .Take(10)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    IQueryable<TGrain> Search<TGrain>() where TGrain : IGrain;
}
