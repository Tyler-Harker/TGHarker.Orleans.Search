namespace TGHarker.Orleans.Search.Abstractions.Abstractions;

/// <summary>
/// Base interface for all generated search entity classes.
/// Provides common properties for tracking grain state in the search database.
/// </summary>
public interface ISearchEntity
{
    /// <summary>
    /// The grain identifier (key) this entity represents.
    /// </summary>
    string GrainId { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// Ensures that out-of-order events don't overwrite newer state.
    /// </summary>
    long Version { get; set; }

    /// <summary>
    /// Timestamp when this entity was last updated in the search index.
    /// </summary>
    DateTime LastUpdated { get; set; }
}
