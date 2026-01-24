using System;
using Orleans;
using TGHarker.Orleans.Search.Core.Query;

namespace TGHarker.Orleans.Search.Orleans;

/// <summary>
/// Implementation of ISearchClient for searching Orleans grains.
/// </summary>
/// <remarks>
/// This class is obsolete. Use <see cref="IClusterClient"/> with the Search extension methods instead.
/// After calling <c>AddOrleansSearch()</c>, the injected <see cref="IClusterClient"/> will support search operations
/// via <see cref="SearchableClusterClient"/>.
/// </remarks>
[Obsolete("Use IClusterClient.Search<TGrain>() instead. Inject IClusterClient and use the generated Search extension methods. This class will be removed in a future version.")]
public class SearchClient : ISearchClient
{
    private readonly IClusterClient _clusterClient;
    private readonly ISearchProviderResolver _resolver;
    private readonly IServiceProvider _serviceProvider;

    public SearchClient(
        IClusterClient clusterClient,
        ISearchProviderResolver resolver,
        IServiceProvider serviceProvider)
    {
        _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IQueryable<TGrain> Search<TGrain>() where TGrain : IGrain
    {
        var provider = _resolver.GetProvider<TGrain>(_serviceProvider);
        if (provider != null)
        {
            return new OrleansQueryable<TGrain>(provider, _clusterClient);
        }

        throw new InvalidOperationException(
            $"No search provider registered for grain type {typeof(TGrain).Name}. " +
            $"Ensure the grain's state class is marked with [Queryable] and the search provider is registered.");
    }
}
