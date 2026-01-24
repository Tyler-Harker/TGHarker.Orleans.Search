namespace TGHarker.Orleans.Search.Abstractions.Attributes;

/// <summary>
/// Marks a string property for full-text search indexing.
/// The property must also be marked with [Queryable] either directly or via class-level attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FullTextSearchableAttribute : Attribute
{
    /// <summary>
    /// Weight for ranking in search results. Higher values boost this field's importance.
    /// Default: 1.0
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Language for text processing and stemming (e.g., "english", "spanish").
    /// Default: "english"
    /// </summary>
    public string Language { get; set; } = "english";

    /// <summary>
    /// Initializes a new instance of the FullTextSearchableAttribute class.
    /// </summary>
    public FullTextSearchableAttribute()
    {
    }
}
