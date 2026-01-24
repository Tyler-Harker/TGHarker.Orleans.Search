using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration.Overrides;
using Orleans.Storage;

namespace TGHarker.Orleans.Search.Orleans.Storage;

/// <summary>
/// Factory for creating SearchableGrainStorage instances.
/// </summary>
public class SearchableGrainStorageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SearchableGrainStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the SearchableGrainStorageFactory class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="logger">The logger instance.</param>
    public SearchableGrainStorageFactory(
        IServiceProvider serviceProvider,
        ILogger<SearchableGrainStorage> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new SearchableGrainStorage instance.
    /// </summary>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="innerStorage">The inner storage provider to wrap.</param>
    /// <returns>A new SearchableGrainStorage instance.</returns>
    public IGrainStorage Create(string name, IGrainStorage innerStorage)
    {
        return new SearchableGrainStorage(innerStorage, _serviceProvider, _logger);
    }
}

/// <summary>
/// Options for configuring SearchableGrainStorage.
/// </summary>
public class SearchableGrainStorageOptions
{
    /// <summary>
    /// The name of the inner storage provider to wrap.
    /// </summary>
    public string InnerStorageProvider { get; set; } = "Default";
}
