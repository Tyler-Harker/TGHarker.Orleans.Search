using System.Text;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator.Generators;

/// <summary>
/// Generates search model classes that provide a property-based interface for querying grains.
/// These models implement ISearchModel&lt;TGrain&gt; and have properties matching the queryable state properties.
/// </summary>
internal static class SearchModelGenerator
{
    public static string Generate(QueryableStateInfo stateInfo)
    {
        if (stateInfo.GrainInterfaceType == null)
            return string.Empty;

        var builder = new StringBuilder();

        builder.AppendLine("using Orleans;");
        builder.AppendLine("using TGHarker.Orleans.Search.Abstractions.Abstractions;");
        builder.AppendLine();
        builder.AppendLine($"namespace {stateInfo.StateNamespace}.Generated;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine($"/// Search model for querying {stateInfo.GrainInterfaceName} grains.");
        builder.AppendLine("/// Use this type in Where expressions for type-safe filtering.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("/// <example>");
        builder.AppendLine($"/// var results = await searchClient.Search&lt;{stateInfo.GrainInterfaceName}&gt;(");
        builder.AppendLine($"///     u => u.{stateInfo.QueryableProperties[0].PropertyName} == \"value\")");
        builder.AppendLine("///     .ToListAsync();");
        builder.AppendLine("/// </example>");
        builder.AppendLine($"public class {GetSearchModelName(stateInfo)} : ISearchModel<{stateInfo.GrainInterfaceFullName}>");
        builder.AppendLine("{");

        // Generate properties matching the queryable state properties
        foreach (var prop in stateInfo.QueryableProperties)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// Queryable property mapped from {stateInfo.StateTypeName}.{prop.PropertyName}");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public {prop.PropertyType} {prop.PropertyName} {{ get; set; }} = default!;");
            builder.AppendLine();
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    public static string GetSearchModelName(QueryableStateInfo stateInfo)
    {
        // UserState -> UserSearch
        // ProjectState -> ProjectSearch
        var stateName = stateInfo.StateTypeName;
        if (stateName.EndsWith("State"))
        {
            return stateName.Substring(0, stateName.Length - 5) + "Search";
        }
        return stateName + "Search";
    }
}
