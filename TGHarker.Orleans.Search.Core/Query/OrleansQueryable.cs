using System.Collections;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Core.Query;

/// <summary>
/// IQueryable implementation for querying Orleans grains through the search index.
/// </summary>
/// <typeparam name="TGrain">The grain interface type.</typeparam>
public class OrleansQueryable<TGrain> : IOrderedQueryable<TGrain>
    where TGrain : IGrain
{
    private readonly IQueryProvider _provider;
    private readonly Expression _expression;

    /// <summary>
    /// Initializes a new instance with a typed search provider.
    /// </summary>
    /// <param name="searchProvider">The search provider instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    public OrleansQueryable(ISearchProvider<TGrain, object> searchProvider, IClusterClient clusterClient)
    {
        _provider = new OrleansQueryProvider<TGrain>(searchProvider, clusterClient);
        _expression = Expression.Constant(this);
    }

    /// <summary>
    /// Initializes a new instance with an untyped search provider.
    /// </summary>
    /// <param name="searchProvider">The search provider instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    public OrleansQueryable(object searchProvider, IClusterClient clusterClient)
    {
        // Cast the provider - it should implement ISearchProvider<TGrain, TState> for some TState
        if (searchProvider == null)
            throw new ArgumentNullException(nameof(searchProvider));

        _provider = new OrleansQueryProvider<TGrain>(searchProvider, clusterClient);
        _expression = Expression.Constant(this);
    }

    /// <summary>
    /// Initializes a new instance with an untyped search provider and service scope.
    /// </summary>
    /// <param name="searchProvider">The search provider instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="scope">The service scope for scoped services like DbContext.</param>
    public OrleansQueryable(object searchProvider, IClusterClient clusterClient, IServiceScope? scope)
    {
        // Cast the provider - it should implement ISearchProvider<TGrain, TState> for some TState
        if (searchProvider == null)
            throw new ArgumentNullException(nameof(searchProvider));

        _provider = new OrleansQueryProvider<TGrain>(searchProvider, clusterClient, scope);
        _expression = Expression.Constant(this);
    }

    /// <summary>
    /// Initializes a new instance from an existing provider and expression.
    /// </summary>
    /// <param name="provider">The query provider.</param>
    /// <param name="expression">The LINQ expression.</param>
    public OrleansQueryable(IQueryProvider provider, Expression expression)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <inheritdoc />
    public Type ElementType => typeof(TGrain);

    /// <inheritdoc />
    public Expression Expression => _expression;

    /// <inheritdoc />
    public IQueryProvider Provider => _provider;

    /// <inheritdoc />
    public IEnumerator<TGrain> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<TGrain>>(_expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
