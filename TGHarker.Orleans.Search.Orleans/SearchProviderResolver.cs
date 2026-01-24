using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.Orleans;

/// <summary>
/// Resolves search providers for grain types from the DI container.
/// </summary>
public class SearchProviderResolver : ISearchProviderResolver
{
    private readonly Assembly[] _assembliesToScan;

    /// <summary>
    /// Initializes a new instance of the SearchProviderResolver class.
    /// </summary>
    /// <param name="assembliesToScan">Assemblies to scan for searchable state types.</param>
    public SearchProviderResolver(Assembly[] assembliesToScan)
    {
        _assembliesToScan = assembliesToScan ?? Array.Empty<Assembly>();
    }

    /// <inheritdoc />
    public object? GetProvider<TGrain>(IServiceProvider serviceProvider) where TGrain : IGrain
    {
        var grainType = typeof(TGrain);

        Console.WriteLine($"[SearchProviderResolver] Looking for provider for grain: {grainType.Name}");
        Console.WriteLine($"[SearchProviderResolver] Assemblies to scan: {_assembliesToScan.Length}");
        foreach (var asm in _assembliesToScan)
        {
            Console.WriteLine($"[SearchProviderResolver]   - {asm.GetName().Name}");
        }

        // Strategy 1: Try to find state type by naming convention (IUserGrain -> UserState)
        var grainName = grainType.Name;
        if (grainName.StartsWith("I") && grainName.EndsWith("Grain"))
        {
            var stateName = grainName.Substring(1, grainName.Length - 6) + "State";
            Console.WriteLine($"[SearchProviderResolver] Looking for state type: {stateName}");

            foreach (var assembly in _assembliesToScan)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var stateType = types.FirstOrDefault(t =>
                        t.Name == stateName &&
                        HasSearchableAttribute(t));

                    if (stateType != null)
                    {
                        Console.WriteLine($"[SearchProviderResolver] Found state type: {stateType.FullName}");
                        var provider = TryResolveProvider(grainType, stateType, serviceProvider);
                        if (provider != null)
                        {
                            Console.WriteLine($"[SearchProviderResolver] Successfully resolved provider");
                            return provider;
                        }
                        else
                        {
                            Console.WriteLine($"[SearchProviderResolver] Failed to resolve provider from DI");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SearchProviderResolver] Error scanning assembly: {ex.Message}");
                }
            }
        }

        // Strategy 2: Scan all provided assemblies for matching state types with [Searchable]
        foreach (var assembly in _assembliesToScan)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // Look for types with [Searchable] attribute that might be state types
                    if (HasSearchableAttribute(type))
                    {
                        var provider = TryResolveProvider(grainType, type, serviceProvider);
                        if (provider != null)
                        {
                            return provider;
                        }
                    }
                }
            }
            catch
            {
                // Skip assemblies that can't be scanned
            }
        }

        return null;
    }

    private static bool HasSearchableAttribute(Type type)
    {
        return type.GetCustomAttributes(false)
            .Any(attr => attr.GetType().Name == "SearchableAttribute");
    }

    private object? TryResolveProvider(Type grainType, Type stateType, IServiceProvider serviceProvider)
    {
        try
        {
            var providerType = typeof(ISearchProvider<,>).MakeGenericType(grainType, stateType);
            Console.WriteLine($"[SearchProviderResolver] Trying to resolve: {providerType.Name}<{grainType.Name}, {stateType.Name}>");

            // Use the provided service provider directly - it's from the request scope
            var provider = serviceProvider.GetService(providerType);

            Console.WriteLine($"[SearchProviderResolver] Provider resolved: {provider != null}");
            return provider;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SearchProviderResolver] Exception resolving provider: {ex.Message}");
            return null;
        }
    }
}
