using System.Collections;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Core.Query;

/// <summary>
/// IQueryable implementation for querying Orleans grains through the search index.
/// </summary>
public class OrleansQueryable<TGrain> : IOrderedQueryable<TGrain>
    where TGrain : IGrain
{
    private readonly IQueryProvider _provider;
    private readonly Expression _expression;

    public OrleansQueryable(ISearchProvider<TGrain, object> searchProvider, IClusterClient clusterClient)
    {
        _provider = new OrleansQueryProvider<TGrain>(searchProvider, clusterClient);
        _expression = Expression.Constant(this);
    }

    public OrleansQueryable(object searchProvider, IClusterClient clusterClient)
    {
        // Cast the provider - it should implement ISearchProvider<TGrain, TState> for some TState
        if (searchProvider == null)
            throw new ArgumentNullException(nameof(searchProvider));

        _provider = new OrleansQueryProvider<TGrain>(searchProvider, clusterClient);
        _expression = Expression.Constant(this);
    }

    public OrleansQueryable(object searchProvider, IClusterClient clusterClient, IServiceScope? scope)
    {
        // Cast the provider - it should implement ISearchProvider<TGrain, TState> for some TState
        if (searchProvider == null)
            throw new ArgumentNullException(nameof(searchProvider));

        _provider = new OrleansQueryProvider<TGrain>(searchProvider, clusterClient, scope);
        _expression = Expression.Constant(this);
    }

    public OrleansQueryable(IQueryProvider provider, Expression expression)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public Type ElementType => typeof(TGrain);

    public Expression Expression => _expression;

    public IQueryProvider Provider => _provider;

    public IEnumerator<TGrain> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<TGrain>>(_expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
