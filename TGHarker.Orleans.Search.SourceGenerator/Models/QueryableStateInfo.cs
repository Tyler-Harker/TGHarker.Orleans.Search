using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TGHarker.Orleans.Search.SourceGenerator.Models;

/// <summary>
/// Information about a searchable grain state class discovered during source generation.
/// Classes must be marked with [Searchable(typeof(IGrain))] and properties with [Queryable].
/// </summary>
internal sealed class QueryableStateInfo
{
    public INamedTypeSymbol StateType { get; }
    public string StateTypeName { get; }
    public string StateNamespace { get; }
    public List<QueryablePropertyInfo> QueryableProperties { get; }
    public INamedTypeSymbol? GrainInterfaceType { get; }
    public string? GrainInterfaceName { get; }
    public string? GrainInterfaceNamespace { get; }
    public string? GrainInterfaceFullName { get; }

    public QueryableStateInfo(
        INamedTypeSymbol stateType,
        List<QueryablePropertyInfo> queryableProperties,
        INamedTypeSymbol? grainInterfaceType = null)
    {
        StateType = stateType;
        StateTypeName = stateType.Name;
        StateNamespace = stateType.ContainingNamespace.ToDisplayString();
        QueryableProperties = queryableProperties;
        GrainInterfaceType = grainInterfaceType;
        GrainInterfaceName = grainInterfaceType?.Name;
        GrainInterfaceNamespace = grainInterfaceType?.ContainingNamespace?.ToDisplayString();
        GrainInterfaceFullName = grainInterfaceType?.ToDisplayString();
    }
}

/// <summary>
/// Information about a queryable property within a grain state class.
/// </summary>
internal sealed class QueryablePropertyInfo
{
    public string PropertyName { get; }
    public string PropertyType { get; }
    public bool IsIndexed { get; }
    public string? IndexName { get; }
    public bool IsFullTextSearchable { get; }
    public double FullTextWeight { get; }
    public string FullTextLanguage { get; }

    public QueryablePropertyInfo(
        string propertyName,
        string propertyType,
        bool isIndexed = true,
        string? indexName = null,
        bool isFullTextSearchable = false,
        double fullTextWeight = 1.0,
        string fullTextLanguage = "english")
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
        IsIndexed = isIndexed;
        IndexName = indexName;
        IsFullTextSearchable = isFullTextSearchable;
        FullTextWeight = fullTextWeight;
        FullTextLanguage = fullTextLanguage;
    }
}
