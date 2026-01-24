using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Storage;
using TGHarker.Orleans.Search.Orleans.Storage;

namespace TGHarker.Orleans.Search.Orleans.Extensions;

/// <summary>
/// Extension methods for configuring searchable grain storage on the silo.
/// </summary>
public static class SiloBuilderSearchExtensions
{
    /// <summary>
    /// Wraps an existing grain storage provider with searchable storage that automatically
    /// syncs grain state changes to the search database.
    /// </summary>
    /// <param name="siloBuilder">The silo builder</param>
    /// <param name="innerStorageName">The name of the inner storage provider to wrap</param>
    /// <param name="name">The name for the searchable storage (defaults to "Default")</param>
    /// <returns>The silo builder for chaining</returns>
    /// <example>
    /// siloBuilder.AddAzureBlobGrainStorage("BlobStorage", ...);
    /// siloBuilder.AddSearchableGrainStorage("BlobStorage");
    /// </example>
    public static ISiloBuilder AddSearchableGrainStorage(
        this ISiloBuilder siloBuilder,
        string innerStorageName,
        string name = ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME)
    {
        siloBuilder.Services.AddKeyedSingleton<IGrainStorage>(name, (sp, _) =>
        {
            var innerStorage = sp.GetRequiredKeyedService<IGrainStorage>(innerStorageName);
            var logger = sp.GetRequiredService<ILogger<SearchableGrainStorage>>();
            return new SearchableGrainStorage(innerStorage, sp, logger);
        });

        return siloBuilder;
    }

    /// <summary>
    /// Wraps the default grain storage provider with searchable storage.
    /// </summary>
    /// <param name="siloBuilder">The silo builder</param>
    /// <param name="innerStorageName">The name of the inner storage provider to wrap</param>
    /// <returns>The silo builder for chaining</returns>
    public static ISiloBuilder AddDefaultSearchableGrainStorage(
        this ISiloBuilder siloBuilder,
        string innerStorageName)
    {
        return siloBuilder.AddSearchableGrainStorage(innerStorageName, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME);
    }
}
