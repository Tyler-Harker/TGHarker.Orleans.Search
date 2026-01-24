using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using TGHarker.Orleans.Search.Orleans.Storage;

namespace TGHarker.Orleans.Search.Orleans.Extensions;

/// <summary>
/// Extension methods for configuring searchable grain storage on the silo.
/// </summary>
public static class SiloBuilderExtensions
{
    /// <summary>
    /// Adds searchable grain storage that wraps another storage provider.
    /// When grain state is written, it's automatically synced to the search database.
    /// </summary>
    /// <param name="builder">The silo builder.</param>
    /// <param name="name">The name of this storage provider.</param>
    /// <param name="configureInnerStorage">Action to configure the inner storage provider.</param>
    /// <returns>The silo builder for chaining.</returns>
    public static ISiloBuilder AddSearchableGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<ISiloBuilder, string> configureInnerStorage)
    {
        // Configure the inner storage with a prefixed name
        var innerName = $"{name}_Inner";
        configureInnerStorage(builder, innerName);

        // Register our searchable storage that wraps the inner one
        builder.Services.AddKeyedSingleton<IGrainStorage>(name, (sp, key) =>
        {
            var innerStorage = sp.GetRequiredKeyedService<IGrainStorage>(innerName);
            var logger = sp.GetRequiredService<ILogger<SearchableGrainStorage>>();
            return new SearchableGrainStorage(innerStorage, sp, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds searchable grain storage as the default storage provider.
    /// </summary>
    /// <param name="builder">The silo builder.</param>
    /// <param name="configureInnerStorage">Action to configure the inner storage provider.</param>
    /// <returns>The silo builder for chaining.</returns>
    public static ISiloBuilder AddSearchableGrainStorageAsDefault(
        this ISiloBuilder builder,
        Action<ISiloBuilder, string> configureInnerStorage)
    {
        return builder.AddSearchableGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureInnerStorage);
    }
}
