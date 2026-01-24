using System.Linq;
using System.Text;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator.Generators;

internal static class EntityGenerator
{
    public static string Generate(QueryableStateInfo stateInfo)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using System.ComponentModel.DataAnnotations;");
        builder.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        builder.AppendLine("using TGHarker.Orleans.Search.Abstractions.Abstractions;");
        builder.AppendLine();
        builder.AppendLine($"namespace {stateInfo.StateNamespace}.Generated;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine($"/// Generated entity class for {stateInfo.StateTypeName} search indexing.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine($"[Table(\"{stateInfo.StateTypeName}s\")]");
        builder.AppendLine($"public class {stateInfo.StateTypeName}Entity : ISearchEntity");
        builder.AppendLine("{");

        // GrainId property (key)
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// The grain identifier.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    [Key]");
        builder.AppendLine("    public string GrainId { get; set; } = string.Empty;");
        builder.AppendLine();

        // Queryable properties
        foreach (var prop in stateInfo.QueryableProperties)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// Indexed property from {stateInfo.StateTypeName}.{prop.PropertyName}");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public {prop.PropertyType} {prop.PropertyName} {{ get; set; }}");
            builder.AppendLine();
        }

        // SearchVector for full-text search (if any properties are full-text searchable)
        bool hasFullTextSearch = stateInfo.QueryableProperties.Any(p => p.IsFullTextSearchable);
        if (hasFullTextSearch)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// Full-text search vector (PostgreSQL tsvector).");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public string? SearchVector { get; set; }");
            builder.AppendLine();
        }

        // Metadata properties
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Version number for optimistic concurrency control.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public long Version { get; set; }");
        builder.AppendLine();

        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Timestamp when this entity was last updated.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public DateTime LastUpdated { get; set; }");

        builder.AppendLine("}");

        return builder.ToString();
    }
}
