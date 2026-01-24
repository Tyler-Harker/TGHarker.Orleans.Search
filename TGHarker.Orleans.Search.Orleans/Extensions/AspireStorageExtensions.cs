using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace TGHarker.Orleans.Search.Orleans.Extensions;

/// <summary>
/// Extension methods for configuring Orleans grain storage with Aspire-provided clients.
/// </summary>
public static class AspireStorageExtensions
{
    /// <summary>
    /// Adds Azure Blob grain storage using an Aspire-provided BlobServiceClient.
    /// </summary>
    /// <param name="siloBuilder">The silo builder</param>
    /// <param name="name">The name for this storage provider</param>
    /// <param name="aspireConnectionName">The Aspire connection name for the blob service</param>
    /// <returns>The silo builder for chaining</returns>
    /// <example>
    /// // In AppHost:
    /// builder.AddKeyedAzureBlobServiceClient("grainstate");
    ///
    /// // In Silo:
    /// siloBuilder.AddAspireAzureBlobGrainStorage("BlobStorage", "grainstate");
    /// </example>
    public static ISiloBuilder AddAspireAzureBlobGrainStorage(
        this ISiloBuilder siloBuilder,
        string name,
        string aspireConnectionName)
    {
        siloBuilder.AddAzureBlobGrainStorage(name, optionsBuilder =>
        {
            optionsBuilder.Configure<IServiceProvider>((options, sp) =>
            {
                var blobClient = sp.GetRequiredKeyedService<BlobServiceClient>(aspireConnectionName);
                options.BlobServiceClient = blobClient;
            });
        });

        return siloBuilder;
    }
}
