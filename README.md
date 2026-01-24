# TGHarker.Orleans.Search

A .NET library that enables **full-text and indexed search capabilities for Microsoft Orleans grains** by automatically maintaining a synchronized search index in PostgreSQL.

## The Problem

Orleans grains are virtual actors accessed by their identity keys. But what if you need to find grains by their state properties?

- *"Find all users with email ending in @example.com"*
- *"Find products priced between $100 and $500"*
- *"Find all orders with status 'Pending'"*

Without a search solution, you'd need to:
1. Maintain a separate index manually
2. Keep it synchronized with grain state
3. Build query infrastructure from scratch

## The Solution

TGHarker.Orleans.Search provides:

- **Automatic Synchronization** - State changes are automatically synced to the search index
- **LINQ Queries** - Familiar `Where`, `FirstOrDefault`, `Count` patterns
- **Source Generation** - Zero boilerplate; just add attributes
- **PostgreSQL Backend** - Leverages EF Core and PostgreSQL full-text search

## Quick Start

### 1. Install Packages

```bash
dotnet add package TGHarker.Orleans.Search
dotnet add package TGHarker.Orleans.Search.PostgreSQL
```

### 2. Mark Your State as Searchable

Add `[Queryable]` only to the properties you need to search on. **You don't need to mark every property** - only the ones you want to query. Properties without `[Queryable]` are stored normally but won't be indexed for search.

```csharp
[Searchable(typeof(IUserGrain))]
[GenerateSerializer]
public class UserState
{
    [Queryable]                              // ✅ Indexed - can search by email
    [Id(0)]
    public string Email { get; set; } = string.Empty;

    [Queryable]                              // ✅ Indexed - can search by name
    [FullTextSearchable(Weight = 2.0)]       // ✅ Also supports full-text search
    [Id(1)]
    public string DisplayName { get; set; } = string.Empty;

    [Queryable]                              // ✅ Indexed - can filter by active status
    [Id(2)]
    public bool IsActive { get; set; }

    [Id(3)]                                  // ❌ NOT indexed - stored but not searchable
    public string InternalNotes { get; set; } = string.Empty;

    [Id(4)]                                  // ❌ NOT indexed - stored but not searchable
    public DateTime LastLoginUtc { get; set; }
}
```

> **Tip**: Only index what you need. Each `[Queryable]` property adds to database storage and sync overhead. Keep your search index lean for better performance.

### 3. Configure the Silo

```csharp
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    // Your existing grain storage
    siloBuilder.AddMemoryGrainStorage("InnerStorage");

    // Wrap with searchable storage
    siloBuilder.AddSearchableGrainStorage("InnerStorage");
});

// Add search services
builder.Services.AddOrleansSearch()
    .UsePostgreSql("Host=localhost;Database=mydb;Username=postgres;Password=postgres");
```

### 4. Query Your Grains

```csharp
// Get the searchable client
var client = serviceProvider.GetRequiredService<ISearchableClusterClient>();

// Search by email
var user = await client.Search<IUserGrain>()
    .Where(u => u.Email == "alice@example.com")
    .FirstOrDefaultAsync();

// Search with Contains
var users = await client.Search<IUserGrain>()
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();

// Filter by boolean
var activeUsers = await client.Search<IUserGrain>()
    .Where(u => u.IsActive == true)
    .ToListAsync();
```

## Features

### Attributes

| Attribute | Description |
|-----------|-------------|
| `[Searchable(typeof(IGrain))]` | Marks a state class as searchable, linking it to a grain interface |
| `[Queryable]` | **Opt-in**: Includes a property in the search index. Properties without this attribute are stored normally but not searchable. |
| `[Queryable(Indexed = true)]` | Creates a database index for faster filtering on high-cardinality columns |
| `[FullTextSearchable]` | Enables PostgreSQL full-text search on string properties |
| `[FullTextSearchable(Weight = 2.0)]` | Sets relevance weight for ranking in full-text results |

> **Note**: `[Queryable]` is opt-in by design. Only mark properties you need to search - this keeps your index small and fast.

### Supported Property Types

- `string`, `bool`, `int`, `long`, `short`, `byte`
- `decimal`, `double`, `float`
- `DateTime`, `DateTimeOffset`, `Guid`

### Query Methods

```csharp
// List all matching
await client.Search<IGrain>().Where(...).ToListAsync();

// First match or null
await client.Search<IGrain>().Where(...).FirstOrDefaultAsync();

// First match or throw
await client.Search<IGrain>().Where(...).FirstAsync();

// Count matches
await client.Search<IGrain>().Where(...).CountAsync();

// Check existence
await client.Search<IGrain>().Where(...).AnyAsync();
```

### Query Expressions

```csharp
// Equality
.Where(u => u.Email == "value")

// Contains (LIKE %value%)
.Where(u => u.Name.Contains("value"))

// Comparison
.Where(p => p.Price >= 100)
.Where(p => p.Price <= 500)

// Combined
.Where(p => p.Price >= 100 && p.Price <= 500 && p.InStock == true)

// Boolean
.Where(u => u.IsActive == true)
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Your Application                            │
├─────────────────────────────────────────────────────────────────┤
│  ISearchableClusterClient.Search<TGrain>().Where(...).ToListAsync()
├─────────────────────────────────────────────────────────────────┤
│                    OrleansQueryProvider                          │
│            (Translates LINQ to EF Core queries)                  │
├─────────────────────────────────────────────────────────────────┤
│                  ISearchProvider<TGrain, TState>                 │
│                    (Generated per grain type)                    │
├─────────────────────────────────────────────────────────────────┤
│                   SearchableGrainStorage                         │
│        (Intercepts writes, syncs to search index)                │
├─────────────────────────────────────────────────────────────────┤
│                  PostgreSqlSearchContext                         │
│                     (EF Core DbContext)                          │
├─────────────────────────────────────────────────────────────────┤
│                       PostgreSQL                                 │
│              (Search index + full-text search)                   │
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

| Project | Description |
|---------|-------------|
| `TGHarker.Orleans.Search.Abstractions` | Core interfaces and attributes |
| `TGHarker.Orleans.Search.Core` | Query provider and base implementations |
| `TGHarker.Orleans.Search.Orleans` | Orleans integration (storage, client extensions) |
| `TGHarker.Orleans.Search.PostgreSQL` | PostgreSQL/EF Core implementation |
| `TGHarker.Orleans.Search.SourceGenerator` | Generates entities, providers, and extensions |
| `TGHarker.Orleans.Search` | Meta package |

## Samples

See the [Samples](./Samples/) folder for complete working examples including:

- User management with searchable properties
- E-commerce products with price ranges
- Order tracking with status filtering

## How It Works

1. **At Build Time**: The source generator scans for `[Searchable]` attributes and generates:
   - Entity classes for EF Core
   - Search providers with property mapping
   - DI registration extension methods
   - Type-safe `Where` extension methods

2. **At Runtime (Writes)**: When `grain.WriteStateAsync()` is called:
   - `SearchableGrainStorage` intercepts the write
   - Calls the inner storage (your actual persistence)
   - Syncs state to the PostgreSQL search index

3. **At Runtime (Queries)**: When you call `Search<IGrain>().Where(...).ToListAsync()`:
   - The predicate is translated to an EF Core query
   - Executed against PostgreSQL
   - Returns grain IDs
   - IDs are materialized into grain references

## Requirements

- .NET 10.0+
- Microsoft Orleans 10.0+
- PostgreSQL 12+

## Known Limitations

- All `[Searchable]` states in an assembly must be in the same namespace
- Only string-keyed grains (`IGrainWithStringKey`) are currently supported
- Sync errors are logged but don't fail the main storage operation

## License

MIT

## Contributing

Contributions are welcome! Please open an issue to discuss proposed changes.
