using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using TGHarker.Orleans.Search.Core.Query;

namespace TGHarker.Orleans.Search.Orleans;

/// <summary>
/// Decorator that wraps an Orleans IClusterClient and adds search functionality.
/// </summary>
/// <remarks>
/// This class delegates all standard IClusterClient/IGrainFactory operations to the inner client
/// while providing the Search&lt;TGrain&gt;() method for querying grains by their state.
/// </remarks>
public class SearchableClusterClient : ISearchableClusterClient
{
    private readonly IClusterClient _inner;
    private readonly ISearchProviderResolver _resolver;
    private readonly IServiceScopeFactory _scopeFactory;

    public SearchableClusterClient(
        IClusterClient inner,
        ISearchProviderResolver resolver,
        IServiceScopeFactory scopeFactory)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <inheritdoc />
    public IQueryable<TGrain> Search<TGrain>() where TGrain : IGrain
    {
        // Create a scope to resolve scoped services like ISearchProvider and DbContext
        var scope = _scopeFactory.CreateScope();
        var provider = _resolver.GetProvider<TGrain>(scope.ServiceProvider);
        if (provider != null)
        {
            return new OrleansQueryable<TGrain>(provider, _inner, scope);
        }

        scope.Dispose();
        throw new InvalidOperationException(
            $"No search provider registered for grain type {typeof(TGrain).Name}. " +
            $"Ensure the grain's state class is marked with [Queryable] and the search provider is registered.");
    }

    #region IClusterClient Properties

    /// <inheritdoc />
    public IServiceProvider ServiceProvider => _inner.ServiceProvider;

    #endregion

    #region IGrainFactory Delegation - Generic GetGrain Methods

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey) where TGrainInterface : IGrainWithGuidKey
        => _inner.GetGrain<TGrainInterface>(primaryKey);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey) where TGrainInterface : IGrainWithIntegerKey
        => _inner.GetGrain<TGrainInterface>(primaryKey);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey) where TGrainInterface : IGrainWithStringKey
        => _inner.GetGrain<TGrainInterface>(primaryKey);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithGuidCompoundKey
        => _inner.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string? grainClassNamePrefix = null)
        where TGrainInterface : IGrainWithIntegerCompoundKey
        => _inner.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId)
        where TGrainInterface : IAddressable
        => _inner.GetGrain<TGrainInterface>(grainId);

    // Explicit interface implementation for methods with different constraints
    TGrainInterface IGrainFactory.GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix)
        => _inner.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);

    TGrainInterface IGrainFactory.GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix)
        => _inner.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);

    TGrainInterface IGrainFactory.GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix)
        => _inner.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);

    #endregion

    #region IGrainFactory Delegation - Non-Generic GetGrain Methods

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey, keyExtension);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey, keyExtension);

    /// <inheritdoc />
    public IAddressable GetGrain(GrainId grainId)
        => _inner.GetGrain(grainId);

    /// <inheritdoc />
    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType)
        => _inner.GetGrain(grainId, interfaceType);

    /// <inheritdoc />
    public IAddressable GetGrain(Type grainInterfaceType, IdSpan grainPrimaryKey)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IAddressable GetGrain(Type grainInterfaceType, IdSpan grainPrimaryKey, string grainClassNamePrefix)
        => _inner.GetGrain(grainInterfaceType, grainPrimaryKey, grainClassNamePrefix);

    #endregion

    #region IGrainFactory Delegation - Observer Methods

    /// <inheritdoc />
    public TGrainObserverInterface CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj)
        where TGrainObserverInterface : IGrainObserver
        => _inner.CreateObjectReference<TGrainObserverInterface>(obj);

    /// <inheritdoc />
    public void DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj)
        where TGrainObserverInterface : IGrainObserver
        => _inner.DeleteObjectReference<TGrainObserverInterface>(obj);

    #endregion
}
