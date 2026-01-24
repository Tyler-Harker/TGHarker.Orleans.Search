using Microsoft.Extensions.DependencyInjection;

namespace TGHarker.Orleans.Search.Orleans.Extensions;

/// <summary>
/// Builder for configuring Orleans search services.
/// </summary>
public interface IOrleansSearchBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }
}

/// <summary>
/// Default implementation of <see cref="IOrleansSearchBuilder"/>.
/// </summary>
public class OrleansSearchBuilder : IOrleansSearchBuilder
{
    /// <summary>
    /// Initializes a new instance of the OrleansSearchBuilder class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public OrleansSearchBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
