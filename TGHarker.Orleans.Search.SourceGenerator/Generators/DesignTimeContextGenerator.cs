using System.Collections.Generic;
using System.Linq;
using System.Text;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator.Generators;

/// <summary>
/// Generates a design-time DbContext factory that explicitly registers all entity types.
/// This enables EF Core migrations to work with the dynamically-discovered entities.
/// </summary>
internal static class DesignTimeContextGenerator
{
    public static string Generate(IEnumerable<QueryableStateInfo> states)
    {
        var statesList = states.ToList();
        if (statesList.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine("using Microsoft.EntityFrameworkCore.Design;");
        builder.AppendLine("using TGHarker.Orleans.Search.PostgreSQL;");
        builder.AppendLine();

        // Collect all entity namespaces
        var entityNamespaces = statesList
            .Select(s => s.StateNamespace + ".Generated")
            .Distinct()
            .OrderBy(n => n);

        foreach (var ns in entityNamespaces)
        {
            builder.AppendLine($"using {ns};");
        }

        builder.AppendLine();
        builder.AppendLine($"namespace {statesList[0].StateNamespace}.Generated;");
        builder.AppendLine();

        // Generate the design-time context that explicitly registers entities
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Design-time DbContext that explicitly registers all generated search entities.");
        builder.AppendLine("/// This enables EF Core migrations to discover the entity model.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public class SearchDesignTimeContext : PostgreSqlSearchContext");
        builder.AppendLine("{");
        builder.AppendLine("    public SearchDesignTimeContext(DbContextOptions<PostgreSqlSearchContext> options)");
        builder.AppendLine("        : base(options)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        builder.AppendLine("    {");
        builder.AppendLine("        // Explicitly register all generated entity types");

        foreach (var state in statesList)
        {
            builder.AppendLine($"        modelBuilder.Entity<{state.StateTypeName}Entity>();");
        }

        builder.AppendLine();
        builder.AppendLine("        // Apply configurations");

        foreach (var state in statesList)
        {
            builder.AppendLine($"        modelBuilder.ApplyConfiguration(new {state.StateTypeName}EntityConfiguration());");
        }

        builder.AppendLine();
        builder.AppendLine("        base.OnModelCreating(modelBuilder);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        // Generate the IDesignTimeDbContextFactory
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Design-time factory for creating the search context during EF Core migrations.");
        builder.AppendLine("/// Used by 'dotnet ef migrations' commands.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public class SearchDesignTimeContextFactory : IDesignTimeDbContextFactory<PostgreSqlSearchContext>");
        builder.AppendLine("{");
        builder.AppendLine("    public PostgreSqlSearchContext CreateDbContext(string[] args)");
        builder.AppendLine("    {");
        builder.AppendLine("        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlSearchContext>();");
        builder.AppendLine();
        builder.AppendLine("        // Default connection string for design-time (migrations)");
        builder.AppendLine("        // Override by setting environment variable or passing --connection argument");
        builder.AppendLine("        var connectionString = Environment.GetEnvironmentVariable(\"SEARCH_DB_CONNECTION\")");
        builder.AppendLine("            ?? \"Host=localhost;Database=searchdb;Username=postgres;Password=postgres\";");
        builder.AppendLine();
        builder.AppendLine("        // Parse --connection argument if provided");
        builder.AppendLine("        for (int i = 0; i < args.Length - 1; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (args[i] == \"--connection\")");
        builder.AppendLine("            {");
        builder.AppendLine("                connectionString = args[i + 1];");
        builder.AppendLine("                break;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        optionsBuilder.UseNpgsql(connectionString, npgsql =>");
        builder.AppendLine("        {");
        builder.AppendLine("            // Use the assembly containing the generated types for migrations");
        builder.AppendLine("            npgsql.MigrationsAssembly(typeof(SearchDesignTimeContextFactory).Assembly.GetName().Name);");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine("        return new SearchDesignTimeContext(optionsBuilder.Options);");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }
}
