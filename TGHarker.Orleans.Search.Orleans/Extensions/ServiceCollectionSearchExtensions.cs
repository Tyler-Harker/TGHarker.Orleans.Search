using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Orleans.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register search providers.
/// </summary>
public static class ServiceCollectionSearchExtensions
{
    /// <summary>
    /// Registers core Orleans search services (resolver and client).
    /// Call this before registering individual search providers.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for queryable state types</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOrleansSearch(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        // Register core services
        services.AddSingleton<ISearchProviderResolver>(new SearchProviderResolver(assemblies));

        // Register obsolete ISearchClient for backwards compatibility
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddScoped<ISearchClient, SearchClient>();
#pragma warning restore CS0618

        // Decorate IClusterClient with SearchableClusterClient
        // This allows users to inject IClusterClient and use search directly
        DecorateClusterClient(services);

        return services;
    }

    /// <summary>
    /// Decorates the registered IClusterClient with SearchableClusterClient to enable search functionality.
    /// </summary>
    private static void DecorateClusterClient(IServiceCollection services)
    {
        // Find the existing IClusterClient registration
        var clusterClientDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IClusterClient));

        if (clusterClientDescriptor == null)
        {
            // IClusterClient not yet registered - register a factory that will resolve it later
            // This handles the case where AddOrleansSearch is called before UseOrleansClient
            services.AddSingleton<ISearchableClusterClient>(sp =>
            {
                var inner = sp.GetRequiredService<IClusterClient>();
                var resolver = sp.GetRequiredService<ISearchProviderResolver>();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new SearchableClusterClient(inner, resolver, scopeFactory);
            });
            return;
        }

        // Remove the existing registration
        services.Remove(clusterClientDescriptor);

        // Re-register the original as a keyed service so we can resolve it
        const string innerClientKey = "__inner_cluster_client__";

        if (clusterClientDescriptor.ImplementationInstance != null)
        {
            services.AddKeyedSingleton(innerClientKey, clusterClientDescriptor.ImplementationInstance);
        }
        else if (clusterClientDescriptor.ImplementationFactory != null)
        {
            services.Add(new ServiceDescriptor(
                typeof(IClusterClient),
                innerClientKey,
                (sp, _) => clusterClientDescriptor.ImplementationFactory(sp),
                clusterClientDescriptor.Lifetime));
        }
        else if (clusterClientDescriptor.ImplementationType != null)
        {
            services.Add(new ServiceDescriptor(
                typeof(IClusterClient),
                innerClientKey,
                clusterClientDescriptor.ImplementationType,
                clusterClientDescriptor.Lifetime));
        }

        // Register SearchableClusterClient as ISearchableClusterClient
        services.Add(new ServiceDescriptor(
            typeof(ISearchableClusterClient),
            sp =>
            {
                var inner = sp.GetRequiredKeyedService<IClusterClient>(innerClientKey);
                var resolver = sp.GetRequiredService<ISearchProviderResolver>();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new SearchableClusterClient(inner, resolver, scopeFactory);
            },
            clusterClientDescriptor.Lifetime));

        // Register IClusterClient to resolve to ISearchableClusterClient
        services.Add(new ServiceDescriptor(
            typeof(IClusterClient),
            sp => sp.GetRequiredService<ISearchableClusterClient>(),
            clusterClientDescriptor.Lifetime));
    }

    /// <summary>
    /// Registers a search provider for a specific grain and state type.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type</typeparam>
    /// <typeparam name="TState">The grain state type</typeparam>
    /// <typeparam name="TProvider">The search provider implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    /// <example>
    /// services.AddSearchProvider&lt;IUserGrain, UserState, UserStateSearchProvider&lt;IUserGrain&gt;&gt;();
    /// </example>
    public static IServiceCollection AddSearchProvider<TGrain, TState, TProvider>(this IServiceCollection services)
        where TGrain : IGrain
        where TState : class
        where TProvider : class, ISearchProvider<TGrain, TState>
    {
        services.AddScoped<ISearchProvider<TGrain, TState>, TProvider>();
        return services;
    }

    /// <summary>
    /// Registers a search provider for a specific grain and state type using a factory.
    /// </summary>
    /// <typeparam name="TGrain">The grain interface type</typeparam>
    /// <typeparam name="TState">The grain state type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="factory">Factory function to create the provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSearchProvider<TGrain, TState>(
        this IServiceCollection services,
        Func<IServiceProvider, ISearchProvider<TGrain, TState>> factory)
        where TGrain : IGrain
        where TState : class
    {
        services.AddScoped(factory);
        return services;
    }
}
