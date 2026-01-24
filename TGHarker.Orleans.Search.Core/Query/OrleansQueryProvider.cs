using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Core.Query;

/// <summary>
/// Query provider that executes LINQ queries against the search index and materializes grain references.
/// </summary>
/// <typeparam name="TGrain">The grain interface type.</typeparam>
public class OrleansQueryProvider<TGrain> : IQueryProvider
    where TGrain : IGrain
{
    private readonly object _searchProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IServiceScope? _scope;
    private LambdaExpression? _entityFilter;

    /// <summary>
    /// Initializes a new instance with a typed search provider.
    /// </summary>
    /// <param name="searchProvider">The search provider instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    public OrleansQueryProvider(ISearchProvider<TGrain, object> searchProvider, IClusterClient clusterClient)
    {
        _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
        _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
    }

    /// <summary>
    /// Initializes a new instance with an untyped search provider.
    /// </summary>
    /// <param name="searchProvider">The search provider instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    public OrleansQueryProvider(object searchProvider, IClusterClient clusterClient)
    {
        _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
        _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
    }

    /// <summary>
    /// Initializes a new instance with an untyped search provider and service scope.
    /// </summary>
    /// <param name="searchProvider">The search provider instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="scope">The service scope for scoped services like DbContext.</param>
    public OrleansQueryProvider(object searchProvider, IClusterClient clusterClient, IServiceScope? scope)
    {
        _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
        _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        _scope = scope;
    }

    /// <summary>
    /// Adds a filter expression to be applied to the entity query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to filter.</typeparam>
    /// <param name="predicate">The filter predicate.</param>
    public void AddEntityFilter<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, ISearchEntity
    {
        _entityFilter = predicate;
    }

    /// <inheritdoc />
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(TGrain))
            throw new NotSupportedException($"Only queries of type {typeof(TGrain)} are supported");

        return (IQueryable<TElement>)(object)new OrleansQueryable<TGrain>(this, expression);
    }

    /// <inheritdoc />
    public IQueryable CreateQuery(Expression expression)
    {
        Type elementType = expression.Type.GetGenericArguments()[0];
        Type queryableType = typeof(OrleansQueryable<>).MakeGenericType(elementType);
        return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(Expression expression)
    {
        // Synchronous execution - calls async version
        return ExecuteAsync(expression, CancellationToken.None).GetAwaiter().GetResult()
            is List<TGrain> list
            ? (TResult)(object)list
            : throw new InvalidOperationException("Unexpected result type");
    }

    /// <inheritdoc />
    public object Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    /// <summary>
    /// Executes the query asynchronously and returns grain references.
    /// </summary>
    /// <param name="expression">The LINQ expression to execute.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A list of grain references matching the query.</returns>
    public async Task<List<TGrain>> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
    {
        try
        {
            List<string> grainIds;

            if (_entityFilter != null)
            {
                // Use the filter-based query
                var queryWithFilterMethod = _searchProvider.GetType().GetMethod("QueryWithFilterAsync");
                if (queryWithFilterMethod != null)
                {
                    var task = (Task<List<string>>)queryWithFilterMethod.Invoke(_searchProvider, new object[] { _entityFilter })!;
                    grainIds = await task;
                }
                else
                {
                    // Fallback to unfiltered query
                    grainIds = await GetAllGrainIdsAsync();
                }
            }
            else
            {
                // No filter - get all grain IDs
                grainIds = await GetAllGrainIdsAsync();
            }

            // Materialize grain references
            var grains = grainIds.Select(GetGrainReference).ToList();
            return grains;
        }
        finally
        {
            // Dispose the scope after query execution to release scoped services (DbContext, etc.)
            _scope?.Dispose();
        }
    }

    private async Task<List<string>> GetAllGrainIdsAsync()
    {
        // Try to use GetAllGrainIdsAsync if available
        var getAllMethod = _searchProvider.GetType().GetMethod("GetAllGrainIdsAsync");
        if (getAllMethod != null)
        {
            var task = (Task<List<string>>)getAllMethod.Invoke(_searchProvider, null)!;
            return await task;
        }

        // Fallback to GetEntityDbSet
        var getDbSetMethod = _searchProvider.GetType().GetMethod("GetEntityDbSet",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (getDbSetMethod == null)
            throw new InvalidOperationException($"Search provider type {_searchProvider.GetType().Name} does not have GetEntityDbSet method");

        var dbQuery = (IQueryable)getDbSetMethod.Invoke(_searchProvider, null)!;
        var entities = await dbQuery.Cast<ISearchEntity>().ToListAsync();
        return entities.Select(e => e.GrainId).ToList();
    }

    private TGrain GetGrainReference(string grainId)
    {
        // Simplified - assumes string-keyed grains
        // Full implementation would need to handle different key types
        var grainType = typeof(TGrain);
        var getGrainMethod = typeof(IGrainFactory).GetMethod(nameof(IGrainFactory.GetGrain), new[] { typeof(string), typeof(string) })!;
        var genericMethod = getGrainMethod.MakeGenericMethod(grainType);
        return (TGrain)genericMethod.Invoke(_clusterClient, new object?[] { grainId, null })!;
    }
}
