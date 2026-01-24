using System.Linq.Expressions;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;
using TGHarker.Orleans.Search.Core.Query;

namespace TGHarker.Orleans.Search.Core.Extensions;

/// <summary>
/// Extension methods for OrleansQueryable to support async operations.
/// </summary>
public static class OrleansQueryableExtensions
{
    /// <summary>
    /// Filters the grain search results by entity properties.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type</typeparam>
    /// <typeparam name="TEntity">The search entity type</typeparam>
    /// <param name="source">The search queryable</param>
    /// <param name="predicate">A predicate to filter by entity properties</param>
    /// <returns>A filtered queryable</returns>
    /// <example>
    /// var users = await searchClient.Search&lt;IUserGrain&gt;()
    ///     .WhereEntity&lt;UserStateEntity&gt;(e => e.Email.Contains("@example.com"))
    ///     .ToListAsync();
    /// </example>
    public static IQueryable<TGrain> WhereEntity<TGrain, TEntity>(
        this IQueryable<TGrain> source,
        Expression<Func<TEntity, bool>> predicate)
        where TGrain : IGrain
        where TEntity : class, ISearchEntity
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // Get the provider and add the filter
        if (source.Provider is OrleansQueryProvider<TGrain> orleansProvider)
        {
            orleansProvider.AddEntityFilter(predicate);
            return source;
        }

        throw new InvalidOperationException(
            $"The source queryable must be an OrleansQueryable with an OrleansQueryProvider. " +
            $"Actual provider type: {source.Provider?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Asynchronously executes the query and returns the results as a list.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A list of grain references matching the query.</returns>
    public static async Task<List<TGrain>> ToListAsync<TGrain>(
        this IQueryable<TGrain> source,
        CancellationToken cancellationToken = default)
        where TGrain : IGrain
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Get the provider from the queryable
        if (source.Provider is OrleansQueryProvider<TGrain> orleansProvider)
        {
            return await orleansProvider.ExecuteAsync(source.Expression, cancellationToken);
        }

        throw new InvalidOperationException(
            $"The source queryable must be an OrleansQueryable with an OrleansQueryProvider. " +
            $"Actual provider type: {source.Provider?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Asynchronously returns the first element of the sequence, or a default value if no element is found.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The first grain reference, or null if none found.</returns>
    public static async Task<TGrain?> FirstOrDefaultAsync<TGrain>(
        this IQueryable<TGrain> source,
        CancellationToken cancellationToken = default)
        where TGrain : IGrain
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (source.Provider is not OrleansQueryProvider<TGrain> orleansProvider)
        {
            throw new InvalidOperationException(
                $"The source queryable must be an OrleansQueryable with an OrleansQueryProvider.");
        }

        var results = await orleansProvider.ExecuteAsync(source.Expression, cancellationToken);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Asynchronously returns the first element of the sequence.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The first grain reference.</returns>
    /// <exception cref="InvalidOperationException">The sequence contains no elements.</exception>
    public static async Task<TGrain> FirstAsync<TGrain>(
        this IQueryable<TGrain> source,
        CancellationToken cancellationToken = default)
        where TGrain : IGrain
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (source.Provider is not OrleansQueryProvider<TGrain> orleansProvider)
        {
            throw new InvalidOperationException(
                $"The source queryable must be an OrleansQueryable with an OrleansQueryProvider.");
        }

        var results = await orleansProvider.ExecuteAsync(source.Expression, cancellationToken);
        if (results.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return results.First();
    }

    /// <summary>
    /// Asynchronously returns the number of elements in the sequence.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>The number of matching grains.</returns>
    public static async Task<int> CountAsync<TGrain>(
        this IQueryable<TGrain> source,
        CancellationToken cancellationToken = default)
        where TGrain : IGrain
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (source.Provider is not OrleansQueryProvider<TGrain> orleansProvider)
        {
            throw new InvalidOperationException(
                $"The source queryable must be an OrleansQueryable with an OrleansQueryProvider.");
        }

        var results = await orleansProvider.ExecuteAsync(source.Expression, cancellationToken);
        return results.Count;
    }

    /// <summary>
    /// Asynchronously determines whether the sequence contains any elements.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>True if any grains match; otherwise, false.</returns>
    public static async Task<bool> AnyAsync<TGrain>(
        this IQueryable<TGrain> source,
        CancellationToken cancellationToken = default)
        where TGrain : IGrain
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (source.Provider is not OrleansQueryProvider<TGrain> orleansProvider)
        {
            throw new InvalidOperationException(
                $"The source queryable must be an OrleansQueryable with an OrleansQueryProvider.");
        }

        var results = await orleansProvider.ExecuteAsync(source.Expression, cancellationToken);
        return results.Any();
    }
}
