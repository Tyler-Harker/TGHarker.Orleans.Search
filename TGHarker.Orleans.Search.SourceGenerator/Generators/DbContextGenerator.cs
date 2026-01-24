using System.Collections.Generic;
using System.Linq;
using System.Text;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator.Generators;

internal static class DbContextGenerator
{
    public static string Generate(IEnumerable<QueryableStateInfo> states)
    {
        var statesList = states.ToList();
        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        builder.AppendLine();

        // Group states by namespace
        var statesByNamespace = statesList.GroupBy(s => s.StateNamespace);

        foreach (var namespaceGroup in statesByNamespace)
        {
            // Generate namespace for entities
            builder.AppendLine($"namespace {namespaceGroup.Key}.Generated;");
            builder.AppendLine();

            // Generate IEntityTypeConfiguration implementations for each entity
            foreach (var state in namespaceGroup)
            {
                builder.AppendLine("/// <summary>");
                builder.AppendLine($"/// Entity type configuration for {state.StateTypeName}Entity.");
                builder.AppendLine("/// </summary>");
                builder.AppendLine($"public class {state.StateTypeName}EntityConfiguration : IEntityTypeConfiguration<{state.StateTypeName}Entity>");
                builder.AppendLine("{");
                builder.AppendLine("    public void Configure(EntityTypeBuilder<" + state.StateTypeName + "Entity> builder)");
                builder.AppendLine("    {");

                // Add table name
                builder.AppendLine($"        builder.ToTable(\"{state.StateTypeName}s\");");
                builder.AppendLine();

                // Add indices
                foreach (var prop in state.QueryableProperties.Where(p => p.IsIndexed))
                {
                    var indexName = prop.IndexName ?? $"IX_{state.StateTypeName}_{prop.PropertyName}";
                    builder.AppendLine($"        builder.HasIndex(e => e.{prop.PropertyName}).HasDatabaseName(\"{indexName}\");");
                }

                // Full-text search index
                if (state.QueryableProperties.Any(p => p.IsFullTextSearchable))
                {
                    builder.AppendLine($"        builder.HasIndex(e => e.SearchVector).HasDatabaseName(\"IX_{state.StateTypeName}_SearchVector\");");
                }

                builder.AppendLine("    }");
                builder.AppendLine("}");
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
