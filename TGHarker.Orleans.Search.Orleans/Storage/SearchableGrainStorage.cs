using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Storage;
using TGHarker.Orleans.Search.Abstractions.Abstractions;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Orleans.Search.Orleans.Storage;

/// <summary>
/// A grain storage provider that decorates another storage provider and automatically
/// syncs searchable state to the search database.
/// </summary>
public class SearchableGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly IGrainStorage _innerStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SearchableGrainStorage> _logger;

    public SearchableGrainStorage(
        IGrainStorage innerStorage,
        IServiceProvider serviceProvider,
        ILogger<SearchableGrainStorage> logger)
    {
        _innerStorage = innerStorage;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        // Delegate lifecycle participation to the inner storage if it supports it
        if (_innerStorage is ILifecycleParticipant<ISiloLifecycle> participant)
        {
            participant.Participate(lifecycle);
        }
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        await _innerStorage.ReadStateAsync(stateName, grainId, grainState);
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        // First, write to the inner storage
        await _innerStorage.WriteStateAsync(stateName, grainId, grainState);

        // Then, sync to search if the state type is searchable
        await TrySyncToSearchAsync(grainId, grainState);
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        await _innerStorage.ClearStateAsync(stateName, grainId, grainState);

        // Also remove from search index
        await TryRemoveFromSearchAsync<T>(grainId);
    }

    private async Task TrySyncToSearchAsync<T>(GrainId grainId, IGrainState<T> grainState)
    {
        try
        {
            var stateType = typeof(T);
            var searchableAttr = stateType.GetCustomAttribute<SearchableAttribute>();

            if (searchableAttr == null)
            {
                return; // Not searchable, skip
            }

            // Get the grain interface type from the attribute
            var grainInterfaceType = searchableAttr.GrainInterfaceType;

            // Build the search provider type
            var providerType = typeof(ISearchProvider<,>).MakeGenericType(grainInterfaceType, stateType);

            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetService(providerType);
            if (provider == null)
            {
                _logger.LogDebug(
                    "No search provider registered for grain type {GrainType} with state {StateType}",
                    grainInterfaceType.Name,
                    stateType.Name);
                return;
            }

            // Get the UpsertAsync method
            var upsertMethod = providerType.GetMethod("UpsertAsync");
            if (upsertMethod == null)
            {
                _logger.LogWarning("UpsertAsync method not found on search provider");
                return;
            }

            // Extract the grain ID as string
            var grainIdString = grainId.Key.ToString();

            // Call UpsertAsync
            var task = (Task)upsertMethod.Invoke(provider, new object[]
            {
                grainIdString!,
                grainState.State!,
                1, // Version - could extract from ETag if needed
                DateTime.UtcNow
            })!;

            await task;

            _logger.LogDebug(
                "Synced grain {GrainId} state to search database",
                grainIdString);
        }
        catch (Exception ex)
        {
            // Log but don't fail the write operation
            _logger.LogError(ex, "Failed to sync grain state to search database for grain {GrainId}", grainId);
        }
    }

    private async Task TryRemoveFromSearchAsync<T>(GrainId grainId)
    {
        try
        {
            var stateType = typeof(T);
            var searchableAttr = stateType.GetCustomAttribute<SearchableAttribute>();

            if (searchableAttr == null)
            {
                return;
            }

            var grainInterfaceType = searchableAttr.GrainInterfaceType;
            var providerType = typeof(ISearchProvider<,>).MakeGenericType(grainInterfaceType, stateType);

            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetService(providerType);
            if (provider == null)
            {
                return;
            }

            var deleteMethod = providerType.GetMethod("DeleteAsync");
            if (deleteMethod == null)
            {
                _logger.LogWarning("DeleteAsync method not found on search provider");
                return;
            }

            var grainIdString = grainId.Key.ToString();

            var task = (Task)deleteMethod.Invoke(provider, new object[] { grainIdString! })!;
            await task;

            _logger.LogDebug("Removed grain {GrainId} from search database", grainIdString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove grain from search database for grain {GrainId}", grainId);
        }
    }
}
