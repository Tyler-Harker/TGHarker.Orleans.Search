using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TGHarker.Orleans.Search.SourceGenerator.Generators;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator;

/// <summary>
/// Roslyn source generator that creates search infrastructure from [Searchable] grain state classes.
/// Generates entity classes, search providers, DbContext configurations, and DI extensions.
/// </summary>
[Generator]
public class QueryableGrainGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with [GenerateSerializer] and [Searchable] attributes
        var queryableStates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsSearchableStateCandidate(node),
                transform: static (ctx, _) => GetQueryableStateInfo(ctx))
            .Where(static info => info is not null)!;

        // Combine with compilation
        var compilationAndStates = context.CompilationProvider.Combine(queryableStates.Collect());

        // Generate code
        context.RegisterSourceOutput(compilationAndStates, (spc, source) =>
        {
            var (compilation, states) = source;

            if (states.IsEmpty)
                return;

            foreach (var state in states)
            {
                // Generate entity class
                var entityCode = EntityGenerator.Generate(state);
                spc.AddSource($"{state.StateTypeName}Entity.g.cs", entityCode);

                // Generate search provider
                var providerCode = SearchProviderGenerator.Generate(state, compilation);
                spc.AddSource($"{state.StateTypeName}SearchProvider.g.cs", providerCode);

                // Generate search model (for fluent API)
                if (state.GrainInterfaceType != null)
                {
                    var searchModelCode = SearchModelGenerator.Generate(state);
                    spc.AddSource($"{state.StateTypeName}Search.g.cs", searchModelCode);
                }
            }

            // Generate DbContext partial class
            var dbContextCode = DbContextGenerator.Generate(states);
            spc.AddSource("SearchDbContext.g.cs", dbContextCode);

            // Generate extension methods for DI registration
            var extensionsCode = GenerateExtensions(states);
            spc.AddSource("SearchServiceExtensions.g.cs", extensionsCode);

            // Generate search client extensions (for fluent Where API)
            var statesWithGrain = states.Where(s => s.GrainInterfaceType != null).ToList();
            if (statesWithGrain.Count > 0)
            {
                var searchExtensionsCode = SearchClientExtensionsGenerator.Generate(statesWithGrain);
                spc.AddSource("SearchClientExtensions.g.cs", searchExtensionsCode);
            }
        });
    }

    private static bool IsSearchableStateCandidate(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.AttributeLists.Count > 0;
    }

    private static QueryableStateInfo? GetQueryableStateInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

        if (symbol is not INamedTypeSymbol classSymbol)
            return null;

        // Check for [GenerateSerializer] (Orleans state marker)
        bool hasGenerateSerializer = classSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "GenerateSerializerAttribute");

        if (!hasGenerateSerializer)
            return null;

        // Check for [Searchable] at class level - this is required for code generation
        var searchableAttr = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "SearchableAttribute");

        if (searchableAttr is null)
            return null;

        // Extract grain interface type from [Searchable(typeof(IGrain))] constructor argument
        INamedTypeSymbol? grainInterfaceType = null;
        if (searchableAttr.ConstructorArguments.Length > 0 &&
            searchableAttr.ConstructorArguments[0].Value is INamedTypeSymbol constructorGrainType)
        {
            grainInterfaceType = constructorGrainType;
        }

        // Collect queryable properties - only properties with [Queryable] are included
        var queryableProperties = new List<QueryablePropertyInfo>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;

            // Skip if property has a setter but it's not public
            if (property.SetMethod?.DeclaredAccessibility != Accessibility.Public)
                continue;

            // Check for property-level [Queryable] - required for inclusion
            var propQueryableAttr = property.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "QueryableAttribute");

            if (propQueryableAttr is null)
                continue;

            // Skip complex types (only support primitives and strings for now)
            if (!IsSupportedPropertyType(property.Type))
                continue;

            // Check for [FullTextSearchable]
            var fullTextAttr = property.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "FullTextSearchableAttribute");

            bool isFullTextSearchable = fullTextAttr is not null;
            double fullTextWeight = 1.0;
            string fullTextLanguage = "english";

            if (isFullTextSearchable && fullTextAttr is not null)
            {
                foreach (var namedArg in fullTextAttr.NamedArguments)
                {
                    if (namedArg.Key == "Weight" && namedArg.Value.Value is double weight)
                        fullTextWeight = weight;
                    else if (namedArg.Key == "Language" && namedArg.Value.Value is string lang)
                        fullTextLanguage = lang;
                }
            }

            // Get index configuration
            bool isIndexed = true;
            string? indexName = null;

            if (propQueryableAttr is not null)
            {
                foreach (var namedArg in propQueryableAttr.NamedArguments)
                {
                    if (namedArg.Key == "Indexed" && namedArg.Value.Value is bool indexed)
                        isIndexed = indexed;
                    else if (namedArg.Key == "IndexName" && namedArg.Value.Value is string name)
                        indexName = name;
                }
            }

            queryableProperties.Add(new QueryablePropertyInfo(
                property.Name,
                property.Type.ToDisplayString(),
                isIndexed,
                indexName,
                isFullTextSearchable,
                fullTextWeight,
                fullTextLanguage));
        }

        if (queryableProperties.Count == 0)
            return null;

        return new QueryableStateInfo(classSymbol, queryableProperties, grainInterfaceType);
    }

    private static bool IsSupportedPropertyType(ITypeSymbol type)
    {
        // Support enums
        if (type.TypeKind == TypeKind.Enum)
            return true;

        if (type.SpecialType != SpecialType.None)
        {
            return type.SpecialType switch
            {
                SpecialType.System_String => true,
                SpecialType.System_Boolean => true,
                SpecialType.System_Int32 => true,
                SpecialType.System_Int64 => true,
                SpecialType.System_Double => true,
                SpecialType.System_Decimal => true,
                SpecialType.System_DateTime => true,
                SpecialType.System_Byte => true,
                SpecialType.System_Int16 => true,
                SpecialType.System_Single => true,
                _ => false
            };
        }

        // Check for Guid, DateTimeOffset
        var typeName = type.ToDisplayString();
        return typeName == "System.Guid" ||
               typeName == "System.DateTimeOffset";
    }

    private static string GenerateExtensions(IEnumerable<QueryableStateInfo> states)
    {
        var statesList = states.Where(s => s.GrainInterfaceType != null).ToList();
        if (statesList.Count == 0)
            return string.Empty;

        var builder = new System.Text.StringBuilder();

        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine("using Orleans;");
        builder.AppendLine("using TGHarker.Orleans.Search.Abstractions.Abstractions;");
        builder.AppendLine("using TGHarker.Orleans.Search.Orleans.Extensions;");
        builder.AppendLine("using System.Reflection;");
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
        builder.AppendLine($"namespace {statesList[0].StateNamespace}.Generated;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Generated extension methods for registering search providers.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class GeneratedSearchRegistration");
        builder.AppendLine("{");

        // Generate individual Add methods for each state
        foreach (var state in statesList)
        {
            var searchModelName = state.StateTypeName.EndsWith("State")
                ? state.StateTypeName.Substring(0, state.StateTypeName.Length - 5) + "Search"
                : state.StateTypeName + "Search";

            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    /// Registers the {state.StateTypeName} search provider for querying {state.GrainInterfaceName} grains.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine($"    public static IServiceCollection Add{searchModelName}(this IServiceCollection services)");
            builder.AppendLine("    {");
            builder.AppendLine($"        return services.AddSearchProvider<{state.GrainInterfaceFullName}, {state.StateTypeName}, {state.StateTypeName}SearchProvider<{state.GrainInterfaceFullName}>>();");
            builder.AppendLine("    }");
        }

        // Generate combined AddOrleansSearch method
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Registers Orleans search services and all generated search providers.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <returns>A builder for further configuration (e.g., UsePostgreSql)</returns>");
        builder.AppendLine("    public static IOrleansSearchBuilder AddOrleansSearch(this IServiceCollection services)");
        builder.AppendLine("    {");
        builder.AppendLine($"        // Register core search services using the state assembly");
        builder.AppendLine($"        services.AddOrleansSearch(typeof({statesList[0].StateTypeName}).Assembly);");
        builder.AppendLine();
        builder.AppendLine("        // Register all search providers");

        for (int i = 0; i < statesList.Count; i++)
        {
            var state = statesList[i];
            var searchModelName = state.StateTypeName.EndsWith("State")
                ? state.StateTypeName.Substring(0, state.StateTypeName.Length - 5) + "Search"
                : state.StateTypeName + "Search";

            builder.AppendLine($"        services.Add{searchModelName}();");
        }

        builder.AppendLine();
        builder.AppendLine("        return new OrleansSearchBuilder(services);");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }
}
