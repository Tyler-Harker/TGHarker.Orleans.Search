using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TGHarker.Orleans.Search.Orleans.Extensions;

namespace TGHarker.Orleans.Search.PostgreSQL.Extensions;

/// <summary>
/// Extension methods for configuring PostgreSQL as the Orleans search data provider.
/// </summary>
/// <remarks>
/// For Aspire applications, use Aspire's AddNpgsqlDbContext before AddOrleansSearch:
/// <code>
/// builder.AddNpgsqlDbContext&lt;PostgreSqlSearchContext&gt;("search");
/// builder.Services.AddOrleansSearch();
/// </code>
///
/// For non-Aspire applications, use UsePostgreSql:
/// <code>
/// builder.Services.AddOrleansSearch()
///     .UsePostgreSql(connectionString);
/// </code>
/// </remarks>
public static class PostgreSqlSearchExtensions
{
    /// <summary>
    /// Configures PostgreSQL as the search data provider using a connection string.
    /// </summary>
    /// <param name="builder">The Orleans search builder</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="configureOptions">Optional action to configure DbContext options</param>
    /// <returns>The builder for chaining</returns>
    public static IOrleansSearchBuilder UsePostgreSql(
        this IOrleansSearchBuilder builder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        builder.Services.AddDbContext<PostgreSqlSearchContext>(options =>
        {
            options.UseNpgsql(connectionString);
            configureOptions?.Invoke(options);
        });

        return builder;
    }

    /// <summary>
    /// Configures PostgreSQL as the search data provider using an action to configure the DbContext.
    /// </summary>
    /// <param name="builder">The Orleans search builder</param>
    /// <param name="configureOptions">Action to configure DbContext options</param>
    /// <returns>The builder for chaining</returns>
    public static IOrleansSearchBuilder UsePostgreSql(
        this IOrleansSearchBuilder builder,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        builder.Services.AddDbContext<PostgreSqlSearchContext>(configureOptions);
        return builder;
    }

    /// <summary>
    /// Configures PostgreSQL as the search data provider using a custom DbContext type.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type (must inherit from PostgreSqlSearchContext)</typeparam>
    /// <param name="builder">The Orleans search builder</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="configureOptions">Optional action to configure DbContext options</param>
    /// <returns>The builder for chaining</returns>
    /// <remarks>
    /// Use this overload when you have a source-generated DbContext (e.g., SearchDesignTimeContext)
    /// that has explicit entity configurations for better EF Core tooling support.
    /// The TContext constructor must accept DbContextOptions&lt;PostgreSqlSearchContext&gt;.
    /// </remarks>
    public static IOrleansSearchBuilder UsePostgreSql<TContext>(
        this IOrleansSearchBuilder builder,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : PostgreSqlSearchContext
    {
        // Build options for the base PostgreSqlSearchContext type (what the generated context expects)
        builder.Services.AddDbContext<PostgreSqlSearchContext>(options =>
        {
            options.UseNpgsql(connectionString);
            configureOptions?.Invoke(options);
        });

        // Register TContext to be created using the PostgreSqlSearchContext options
        builder.Services.AddScoped<TContext>(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<PostgreSqlSearchContext>>();
            return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        });

        return builder;
    }

    /// <summary>
    /// Configures PostgreSQL as the search data provider using a custom DbContext type.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type (must inherit from PostgreSqlSearchContext)</typeparam>
    /// <param name="builder">The Orleans search builder</param>
    /// <param name="configureOptions">Action to configure DbContext options</param>
    /// <returns>The builder for chaining</returns>
    /// <remarks>
    /// Use this overload when you have a source-generated DbContext (e.g., SearchDesignTimeContext)
    /// that has explicit entity configurations for better EF Core tooling support.
    /// The TContext constructor must accept DbContextOptions&lt;PostgreSqlSearchContext&gt;.
    /// </remarks>
    public static IOrleansSearchBuilder UsePostgreSql<TContext>(
        this IOrleansSearchBuilder builder,
        Action<DbContextOptionsBuilder> configureOptions)
        where TContext : PostgreSqlSearchContext
    {
        // Build options for the base PostgreSqlSearchContext type (what the generated context expects)
        builder.Services.AddDbContext<PostgreSqlSearchContext>(configureOptions);

        // Register TContext to be created using the PostgreSqlSearchContext options
        builder.Services.AddScoped<TContext>(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<PostgreSqlSearchContext>>();
            return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        });

        return builder;
    }
}
