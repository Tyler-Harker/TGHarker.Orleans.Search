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

    public SearchableGrainStorageFactory(
        IServiceProvider serviceProvider,
        ILogger<SearchableGrainStorage> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

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
