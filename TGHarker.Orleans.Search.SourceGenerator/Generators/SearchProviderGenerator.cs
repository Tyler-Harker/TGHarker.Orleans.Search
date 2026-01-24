using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TGHarker.Orleans.Search.SourceGenerator.Models;

namespace TGHarker.Orleans.Search.SourceGenerator.Generators;

internal static class SearchProviderGenerator
{
    public static string Generate(QueryableStateInfo stateInfo, Compilation compilation)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using System.Linq.Expressions;");
        builder.AppendLine("using System.Reflection;");
        builder.AppendLine("using TGHarker.Orleans.Search.Abstractions.Abstractions;");
        builder.AppendLine("using TGHarker.Orleans.Search.Core.Providers;");
        builder.AppendLine("using TGHarker.Orleans.Search.PostgreSQL;");
        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine();
        builder.AppendLine($"namespace {stateInfo.StateNamespace}.Generated;");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine($"/// Generated search provider for {stateInfo.StateTypeName}.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine($"public class {stateInfo.StateTypeName}SearchProvider<TGrain> : SearchProviderBase<TGrain, {stateInfo.StateTypeName}, {stateInfo.StateTypeName}Entity>");
        builder.AppendLine("    where TGrain : global::Orleans.IGrain");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly PostgreSqlSearchContext _dbContext;");
        builder.AppendLine();
        builder.AppendLine($"    public {stateInfo.StateTypeName}SearchProvider(PostgreSqlSearchContext dbContext)");
        builder.AppendLine("        : base(dbContext)");
        builder.AppendLine("    {");
        builder.AppendLine("        _dbContext = dbContext;");
        builder.AppendLine("    }");
        builder.AppendLine();

        // GetEntityDbSet implementation
        builder.AppendLine($"    protected override IQueryable<{stateInfo.StateTypeName}Entity> GetEntityDbSet()");
        builder.AppendLine($"        => _dbContext.Set<{stateInfo.StateTypeName}Entity>();");
        builder.AppendLine();

        // MapStateToEntity implementation
        builder.AppendLine($"    protected override void MapStateToEntity({stateInfo.StateTypeName} state, {stateInfo.StateTypeName}Entity entity)");
        builder.AppendLine("    {");

        foreach (var prop in stateInfo.QueryableProperties)
        {
            builder.AppendLine($"        entity.{prop.PropertyName} = state.{prop.PropertyName};");
        }

        // Generate full-text search vector if needed
        var fullTextProps = stateInfo.QueryableProperties.Where(p => p.IsFullTextSearchable).ToList();
        if (fullTextProps.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("        // Build full-text search vector");
            builder.AppendLine("        var searchableText = new System.Collections.Generic.List<string>();");

            foreach (var prop in fullTextProps)
            {
                builder.AppendLine($"        if (!string.IsNullOrWhiteSpace(state.{prop.PropertyName}?.ToString()))");
                builder.AppendLine($"            searchableText.Add(state.{prop.PropertyName}.ToString()!);");
            }

            builder.AppendLine("        entity.SearchVector = string.Join(\" \", searchableText);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        // MapGrainPropertyToEntity implementation
        builder.AppendLine("    public override Expression MapGrainPropertyToEntity(MemberInfo member, ParameterExpression entityParameter)");
        builder.AppendLine("    {");
        builder.AppendLine("        return member.Name switch");
        builder.AppendLine("        {");

        foreach (var prop in stateInfo.QueryableProperties)
        {
            builder.AppendLine($"            \"{prop.PropertyName}\" => Expression.Property(entityParameter, nameof({stateInfo.StateTypeName}Entity.{prop.PropertyName})),");
        }

        builder.AppendLine("            _ => throw new NotSupportedException($\"Property {{member.Name}} is not queryable\")");
        builder.AppendLine("        };");
        builder.AppendLine("    }");
        builder.AppendLine();

        // MapGrainMethodToEntityProperty implementation
        builder.AppendLine("    public override Expression MapGrainMethodToEntityProperty(MethodInfo method, ParameterExpression entityParameter)");
        builder.AppendLine("    {");
        builder.AppendLine("        // Map grain interface methods to entity properties");
        builder.AppendLine("        // This requires knowing the grain interface, which we'll handle in the Core library");
        builder.AppendLine("        return MapGrainPropertyToEntity(method, entityParameter);");
        builder.AppendLine("    }");

        builder.AppendLine("}");

        return builder.ToString();
    }
}
