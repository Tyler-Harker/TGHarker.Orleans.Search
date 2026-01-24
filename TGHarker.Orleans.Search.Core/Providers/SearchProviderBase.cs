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
public abstract class SearchProviderBase<TGrain, TState, TEntity> : ISearchProvider<TGrain, TState>
    where TGrain : IGrain
    where TEntity : class, ISearchEntity, new()
{
    protected readonly DbContext DbContext;

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
    public abstract Expression MapGrainPropertyToEntity(MemberInfo member, ParameterExpression entityParameter);

    public abstract Expression MapGrainMethodToEntityProperty(MethodInfo method, ParameterExpression entityParameter);

    public async Task UpsertAsync(string grainId, TState state, long version, DateTime timestamp)
    {
        var existing = await GetEntityDbSet().FirstOrDefaultAsync(e => e.GrainId == grainId);

        if (existing != null && existing.Version >= version)
        {
            // Stale event, ignore
            return;
        }

        if (existing == null)
        {
            existing = new TEntity { GrainId = grainId };
            DbContext.Add(existing);
        }

        MapStateToEntity(state, existing);
        existing.Version = version;
        existing.LastUpdated = timestamp;

        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(string grainId)
    {
        var entity = await GetEntityDbSet().FirstOrDefaultAsync(e => e.GrainId == grainId);

        if (entity != null)
        {
            DbContext.Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }

    public virtual async Task<IEnumerable<string>> FullTextSearchAsync(string query, int maxResults, double minScore = 0.0)
    {
        // Default implementation - subclasses can override for database-specific full-text search
        throw new NotImplementedException("Full-text search must be implemented by database-specific provider");
    }
}
