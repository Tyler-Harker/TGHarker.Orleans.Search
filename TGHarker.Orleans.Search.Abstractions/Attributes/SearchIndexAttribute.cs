namespace TGHarker.Orleans.Search.Abstractions.Attributes;

/// <summary>
/// Configures custom index options for a queryable property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class SearchIndexAttribute : Attribute
{
    /// <summary>
    /// Name of the index.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Index type (e.g., "BTREE", "HASH", "GIN" for PostgreSQL).
    /// If not specified, the database default will be used.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Whether this index enforces uniqueness.
    /// Default: false
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Names of other properties to include in a composite index.
    /// </summary>
    public string[]? CompositeWith { get; set; }

    /// <summary>
    /// Initializes a new instance of the SearchIndexAttribute class.
    /// </summary>
    /// <param name="name">The name of the index.</param>
    public SearchIndexAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
