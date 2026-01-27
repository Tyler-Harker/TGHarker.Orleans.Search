using System.Collections.Generic;
using System.Linq;
using System.Text;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator.Generators;

/// <summary>
/// Generates Search and Where extension methods for fluent LINQ-style queries.
/// Supports both:
///   clusterClient.Search&lt;IUserGrain&gt;(u => u.Email == "test")
///   clusterClient.Search&lt;IUserGrain&gt;().Where(u => u.Email == "test")
/// </summary>
internal static class SearchClientExtensionsGenerator
{
    public static string Generate(IEnumerable<QueryableStateInfo> states)
    {
        var statesList = states.Where(s => s.GrainInterfaceType != null).ToList();
        if (statesList.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();

        builder.AppendLine("using System;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using System.Linq.Expressions;");
        builder.AppendLine("using Orleans;");
        builder.AppendLine("using TGHarker.Orleans.Search.Abstractions.Abstractions;");
        builder.AppendLine("using TGHarker.Orleans.Search.Core.Query;");
        builder.AppendLine("using TGHarker.Orleans.Search.Core.Extensions;");
        builder.AppendLine("using TGHarker.Orleans.Search.Orleans;");
        builder.AppendLine();

        // Collect namespaces
        var namespaces = statesList
            .Select(s => s.StateNamespace)
            .Concat(statesList.Select(s => s.StateNamespace + ".Generated"))
            .Concat(statesList.Where(s => s.GrainInterfaceNamespace != null).Select(s => s.GrainInterfaceNamespace!))
            .Distinct()
            .OrderBy(n => n);

        foreach (var ns in namespaces)
        {
            builder.AppendLine($"using {ns};");
        }

        builder.AppendLine();
        builder.AppendLine("namespace TGHarker.Orleans.Search.Generated;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Generated Search and Where extension methods for fluent grain search queries.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("/// <remarks>");
        builder.AppendLine("/// Usage (direct predicate):");
        builder.AppendLine("/// <code>");
        builder.AppendLine("/// var users = await clusterClient.Search&lt;IUserGrain&gt;(u => u.Email.Contains(\"@example.com\")).ToListAsync();");
        builder.AppendLine("/// </code>");
        builder.AppendLine("/// Usage (chained):");
        builder.AppendLine("/// <code>");
        builder.AppendLine("/// var users = await clusterClient.Search&lt;IUserGrain&gt;()");
        builder.AppendLine("///     .Where(u => u.Email.Contains(\"@example.com\"))");
        builder.AppendLine("///     .ToListAsync();");
        builder.AppendLine("/// </code>");
        builder.AppendLine("/// The lambda parameter 'u' is automatically inferred as the search model type.");
        builder.AppendLine("/// </remarks>");
        builder.AppendLine("public static class GrainSearchExtensions");
        builder.AppendLine("{");

        // Generate a single generic Search<TGrain>() method for all grain types
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Creates a queryable for searching grains of the specified type.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <typeparam name=\"TGrain\">The grain interface type to search for</typeparam>");
        builder.AppendLine("    /// <param name=\"client\">The cluster client</param>");
        builder.AppendLine("    /// <returns>A queryable of grains</returns>");
        builder.AppendLine("    public static IQueryable<TGrain> Search<TGrain>(this IClusterClient client)");
        builder.AppendLine("        where TGrain : IGrain");
        builder.AppendLine("    {");
        builder.AppendLine("        if (client == null)");
        builder.AppendLine("            throw new ArgumentNullException(nameof(client));");
        builder.AppendLine();
        builder.AppendLine("        if (client is ISearchableClusterClient searchable)");
        builder.AppendLine("            return searchable.Search<TGrain>();");
        builder.AppendLine();
        builder.AppendLine("        throw new InvalidOperationException(");
        builder.AppendLine("            \"The IClusterClient is not searchable. Ensure AddOrleansSearch() from your state assembly's .Generated namespace \" +");
        builder.AppendLine("            \"was called during service registration (not AddOrleansSearchCore from TGHarker.Orleans.Search.Orleans.Extensions).\");");
        builder.AppendLine("    }");

        foreach (var state in statesList)
        {
            var searchModelName = SearchModelGenerator.GetSearchModelName(state);
            var grainInterface = state.GrainInterfaceFullName;
            var grainInterfaceName = state.GrainInterfaceName;
            var entityName = $"{state.StateTypeName}Entity";
            var fullSearchModelName = $"{state.StateNamespace}.Generated.{searchModelName}";
            var fullEntityName = $"{state.StateNamespace}.Generated.{entityName}";

            // Generate Search<TGrain>(predicate) extension on IClusterClient
            // Note: The no-predicate Search<TGrain>() is handled by ISearchableClusterClient directly
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// Searches for {state.GrainInterfaceName} grains matching the given predicate.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    /// <param name=\"client\">The cluster client</param>");
            builder.AppendLine($"    /// <param name=\"predicate\">Filter expression using {searchModelName} properties</param>");
            builder.AppendLine($"    /// <returns>A queryable of matching {state.GrainInterfaceName} grains</returns>");
            builder.AppendLine("    /// <example>");
            builder.AppendLine($"    /// var users = await clusterClient.Search&lt;{state.GrainInterfaceName}&gt;(u => u.Email.Contains(\"@example.com\")).ToListAsync();");
            builder.AppendLine("    /// </example>");
            builder.AppendLine($"    public static IQueryable<{grainInterface}> Search<TGrain>(");
            builder.AppendLine($"        this IClusterClient client,");
            builder.AppendLine($"        Expression<Func<{fullSearchModelName}, bool>> predicate)");
            builder.AppendLine($"        where TGrain : {grainInterface}");
            builder.AppendLine("    {");
            builder.AppendLine("        if (client == null)");
            builder.AppendLine("            throw new ArgumentNullException(nameof(client));");
            builder.AppendLine("        if (predicate == null)");
            builder.AppendLine("            throw new ArgumentNullException(nameof(predicate));");
            builder.AppendLine();
            builder.AppendLine("        if (client is ISearchableClusterClient searchable)");
            builder.AppendLine($"            return searchable.Search<{grainInterface}>().Where(predicate);");
            builder.AppendLine();
            builder.AppendLine("        throw new InvalidOperationException(");
            builder.AppendLine("            \"The IClusterClient is not searchable. Ensure AddOrleansSearch() from your state assembly's .Generated namespace \" +");
            builder.AppendLine("            \"was called during service registration (not AddOrleansSearchCore from TGHarker.Orleans.Search.Orleans.Extensions).\");");
            builder.AppendLine("    }");

            // Generate Where extension on IQueryable<TGrain>
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// Filters {state.GrainInterfaceName} grains using a predicate on {searchModelName} properties.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    /// <param name=\"source\">The search queryable from Search&lt;{state.GrainInterfaceName}&gt;()</param>");
            builder.AppendLine($"    /// <param name=\"predicate\">Filter expression using {searchModelName} properties</param>");
            builder.AppendLine($"    /// <returns>A filtered queryable</returns>");
            builder.AppendLine("    /// <example>");
            builder.AppendLine($"    /// var users = await clusterClient.Search&lt;{state.GrainInterfaceName}&gt;()");
            builder.AppendLine($"    ///     .Where(u => u.Email.Contains(\"@example.com\"))");
            builder.AppendLine("    ///     .ToListAsync();");
            builder.AppendLine("    /// </example>");
            builder.AppendLine($"    public static IQueryable<{grainInterface}> Where(");
            builder.AppendLine($"        this IQueryable<{grainInterface}> source,");
            builder.AppendLine($"        Expression<Func<{fullSearchModelName}, bool>> predicate)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (source == null)");
            builder.AppendLine("            throw new ArgumentNullException(nameof(source));");
            builder.AppendLine("        if (predicate == null)");
            builder.AppendLine("            throw new ArgumentNullException(nameof(predicate));");
            builder.AppendLine();
            builder.AppendLine($"        // Translate {searchModelName} predicate to {entityName} predicate");
            builder.AppendLine($"        var entityPredicate = TranslateSearchToEntity_{state.StateTypeName}(predicate);");
            builder.AppendLine($"        return source.WhereEntity<{grainInterface}, {fullEntityName}>(entityPredicate);");
            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate the translation method
            builder.AppendLine($"    private static Expression<Func<{fullEntityName}, bool>> TranslateSearchToEntity_{state.StateTypeName}(");
            builder.AppendLine($"        Expression<Func<{fullSearchModelName}, bool>> searchPredicate)");
            builder.AppendLine("    {");
            builder.AppendLine($"        var entityParam = Expression.Parameter(typeof({fullEntityName}), \"e\");");
            builder.AppendLine($"        var visitor = new SearchToEntityVisitor_{state.StateTypeName}(entityParam);");
            builder.AppendLine("        var translatedBody = visitor.Visit(searchPredicate.Body);");
            builder.AppendLine($"        return Expression.Lambda<Func<{fullEntityName}, bool>>(translatedBody, entityParam);");
            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate the visitor class
            builder.AppendLine($"    private class SearchToEntityVisitor_{state.StateTypeName} : ExpressionVisitor");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly ParameterExpression _entityParam;");
            builder.AppendLine();
            builder.AppendLine($"        public SearchToEntityVisitor_{state.StateTypeName}(ParameterExpression entityParam)");
            builder.AppendLine("        {");
            builder.AppendLine("            _entityParam = entityParam;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        protected override Expression VisitParameter(ParameterExpression node)");
            builder.AppendLine("        {");
            builder.AppendLine($"            if (node.Type == typeof({fullSearchModelName}))");
            builder.AppendLine("                return _entityParam;");
            builder.AppendLine("            return base.VisitParameter(node);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        protected override Expression VisitMember(MemberExpression node)");
            builder.AppendLine("        {");
            builder.AppendLine("            // First visit the expression to handle nested members");
            builder.AppendLine("            var visitedExpression = node.Expression != null ? Visit(node.Expression) : null;");
            builder.AppendLine();
            builder.AppendLine($"            if (node.Expression?.Type == typeof({fullSearchModelName}))");
            builder.AppendLine("            {");
            builder.AppendLine("                // Map search model property to entity property (same names)");
            builder.AppendLine($"                var entityProperty = typeof({fullEntityName}).GetProperty(node.Member.Name);");
            builder.AppendLine("                if (entityProperty != null)");
            builder.AppendLine("                {");
            builder.AppendLine("                    return Expression.Property(_entityParam, entityProperty);");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            // Handle cases where the expression was transformed");
            builder.AppendLine("            if (visitedExpression != null && visitedExpression != node.Expression)");
            builder.AppendLine("            {");
            builder.AppendLine("                return Expression.MakeMemberAccess(visitedExpression, node.Member);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return base.VisitMember(node);");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }
}
