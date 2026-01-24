namespace TGHarker.Orleans.Search.Abstractions.Attributes;

/// <summary>
/// Marks a property as queryable through the search system.
/// Only properties with this attribute will be included in the search index.
/// The containing class must be marked with [Searchable(typeof(IGrain))].
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class QueryableAttribute : Attribute
{
    /// <summary>
    /// Whether to create a database index for this property.
    /// Default: true
    /// </summary>
    public bool Indexed { get; set; } = true;

    /// <summary>
    /// Custom index name. If not specified, a name will be auto-generated.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// Initializes a new instance of the QueryableAttribute class.
    /// </summary>
    public QueryableAttribute()
    {
    }
}
