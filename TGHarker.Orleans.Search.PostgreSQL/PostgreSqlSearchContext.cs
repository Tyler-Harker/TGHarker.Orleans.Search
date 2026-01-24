using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TGHarker.Orleans.Search.Abstractions.Abstractions;

namespace TGHarker.Orleans.Search.PostgreSQL;

/// <summary>
/// Base DbContext for PostgreSQL search database.
/// Automatically discovers and registers entity types from loaded assemblies.
/// </summary>
public class PostgreSqlSearchContext : DbContext
{
    public PostgreSqlSearchContext(DbContextOptions<PostgreSqlSearchContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discover and register all entity types implementing ISearchEntity from loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                // Find all entity types
                var entityTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ISearchEntity).IsAssignableFrom(t));

                foreach (var entityType in entityTypes)
                {
                    // Register entity with EF Core
                    modelBuilder.Entity(entityType);
                }

                // Find and apply all IEntityTypeConfiguration implementations
                var configurationTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract &&
                                t.GetInterfaces().Any(i => i.IsGenericType &&
                                                           i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

                foreach (var configurationType in configurationTypes)
                {
                    var configuration = Activator.CreateInstance(configurationType);
                    if (configuration != null)
                    {
                        // Apply configuration using reflection
                        var applyConfigurationMethod = typeof(ModelBuilder)
                            .GetMethods()
                            .First(m => m.Name == nameof(ModelBuilder.ApplyConfiguration) &&
                                       m.GetParameters().Length == 1 &&
                                       m.GetParameters()[0].ParameterType.IsGenericType);

                        var entityType = configurationType.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                            .GetGenericArguments()[0];

                        var genericMethod = applyConfigurationMethod.MakeGenericMethod(entityType);
                        genericMethod.Invoke(modelBuilder, new[] { configuration });
                    }
                }
            }
            catch
            {
                // Skip assemblies that can't be scanned
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
