using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Core.Providers;

/// <summary>
/// Base class for search provider implementations.
/// Provides common functionality for upserting, deleting, and querying grain state.
/// </summary>
/// <typeparam name="TGrain">The grain interface type.</typeparam>
/// <typeparam name="TState">The grain state type.</typeparam>
/// <typeparam name="TEntity">The EF Core entity type for the search index.</typeparam>
public abstract class SearchProviderBase<TGrain, TState, TEntity> : ISearchProvider<TGrain, TState>
    where TGrain : IGrain
    where TEntity : class, ISearchEntity, new()
{
    /// <summary>
    /// The Entity Framework Core database context.
    /// </summary>
    protected readonly DbContext DbContext;

    /// <summary>
    /// Initializes a new instance of the search provider.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    protected SearchProviderBase(DbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Gets the entity type for this provider.
    /// </summary>
    public Type EntityType => typeof(TEntity);

    /// <summary>
    /// Gets the grain ID property name.
    /// </summary>
    public string GrainIdPropertyName => nameof(ISearchEntity.GrainId);

    /// <summary>
    /// Gets the queryable database set for the entity type.
    /// </summary>
    protected abstract IQueryable<TEntity> GetEntityDbSet();

    IQueryable<object> ISearchProvider<TGrain, TState>.GetDbSet() => GetEntityDbSet().Cast<object>();

    /// <summary>
    /// Gets all grain IDs without filtering.
    /// </summary>
    public virtual async Task<List<string>> GetAllGrainIdsAsync()
    {
        return await GetEntityDbSet()
            .Select(e => e.GrainId)
            .ToListAsync();
    }

    /// <summary>
    /// Queries with a filter expression and returns matching grain IDs.
    /// </summary>
    public virtual async Task<List<string>> QueryWithFilterAsync(LambdaExpression predicate)
    {
        // The predicate should be Expression<Func<TEntity, bool>>
        if (predicate.Parameters.Count != 1 || predicate.Parameters[0].Type != typeof(TEntity))
        {
            throw new ArgumentException(
                $"Predicate must be of type Expression<Func<{typeof(TEntity).Name}, bool>>",
                nameof(predicate));
        }

        var typedPredicate = (Expression<Func<TEntity, bool>>)predicate;

        return await GetEntityDbSet()
            .Where(typedPredicate)
            .Select(e => e.GrainId)
            .ToListAsync();
    }

    /// <summary>
    /// Maps grain state properties to entity properties.
    /// </summary>
    protected abstract void MapStateToEntity(TState state, TEntity entity);

    /// <summary>
    /// Maps a grain property to an entity property expression.
    /// </summary>
    /// <param name="member">The grain property member info.</param>
    /// <param name="entityParameter">The entity parameter expression.</param>
    /// <returns>An expression accessing the corresponding entity property.</returns>
    public abstract Expression MapGrainPropertyToEntity(MemberInfo member, ParameterExpression entityParameter);

    /// <summary>
    /// Maps a grain interface method to an entity property expression.
    /// </summary>
    /// <param name="method">The grain interface method info.</param>
    /// <param name="entityParameter">The entity parameter expression.</param>
    /// <returns>An expression accessing the corresponding entity property.</returns>
    public abstract Expression MapGrainMethodToEntityProperty(MethodInfo method, ParameterExpression entityParameter);

    /// <summary>
    /// Updates or inserts grain state into the search index.
    /// </summary>
    /// <param name="grainId">The grain identifier.</param>
    /// <param name="state">The grain state to index.</param>
    /// <param name="version">Version number for optimistic concurrency. If 0 or less, version will be auto-incremented.</param>
    /// <param name="timestamp">When this update occurred.</param>
    public async Task UpsertAsync(string grainId, TState state, long version, DateTime timestamp)
    {
        var existing = await GetEntityDbSet().FirstOrDefaultAsync(e => e.GrainId == grainId);

        // Auto-increment version if not explicitly provided (version <= 0) or if caller passes a fixed value
        // This ensures updates always succeed when called from the storage decorator
        long newVersion;
        if (existing != null)
        {
            // For updates: only skip if an explicit version was provided and it's stale
            if (version > 0 && existing.Version >= version)
            {
                // Stale event with explicit version, ignore
                return;
            }
            // Auto-increment from existing version
            newVersion = existing.Version + 1;
        }
        else
        {
            existing = new TEntity { GrainId = grainId };
            DbContext.Add(existing);
            newVersion = version > 0 ? version : 1;
        }

        MapStateToEntity(state, existing);
        existing.Version = newVersion;
        existing.LastUpdated = timestamp;

        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a grain from the search index.
    /// </summary>
    /// <param name="grainId">The grain identifier to remove.</param>
    public async Task DeleteAsync(string grainId)
    {
        var entity = await GetEntityDbSet().FirstOrDefaultAsync(e => e.GrainId == grainId);

        if (entity != null)
        {
            DbContext.Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Performs full-text search across indexed text fields.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="minScore">Minimum relevance score (0.0 to 1.0).</param>
    /// <returns>Collection of grain IDs matching the search query.</returns>
    public virtual async Task<IEnumerable<string>> FullTextSearchAsync(string query, int maxResults, double minScore = 0.0)
    {
        // Default implementation - subclasses can override for database-specific full-text search
        throw new NotImplementedException("Full-text search must be implemented by database-specific provider");
    }
}
