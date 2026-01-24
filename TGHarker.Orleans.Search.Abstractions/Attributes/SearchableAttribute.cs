namespace TGHarker.Orleans.Search.Abstractions.Attributes;

/// <summary>
/// Marks a grain state class as searchable through the search system.
/// Properties must be individually marked with [Queryable] to be included in the search index.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SearchableAttribute : Attribute
{
    /// <summary>
    /// The grain interface type that uses this state.
    /// Required to enable automatic search synchronization and query routing.
    /// </summary>
    public Type GrainInterfaceType { get; }

    /// <summary>
    /// Initializes a new instance of the SearchableAttribute class.
    /// </summary>
    /// <param name="grainInterfaceType">The grain interface type that uses this state.</param>
    public SearchableAttribute(Type grainInterfaceType)
    {
        GrainInterfaceType = grainInterfaceType ?? throw new ArgumentNullException(nameof(grainInterfaceType));
    }
}
