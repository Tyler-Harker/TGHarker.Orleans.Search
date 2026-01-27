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

// Add search services - IMPORTANT: use the generated namespace!
using YourNamespace.Models.Generated;  // <-- Use YOUR state namespace + .Generated

builder.Services.AddOrleansSearch()
    .UsePostgreSql("Host=localhost;Database=mydb;Username=postgres;Password=postgres");
```

> **⚠️ Common Mistake:** Do NOT import `TGHarker.Orleans.Search.Orleans.Extensions` for `AddOrleansSearch()`.
> That namespace contains the internal `AddOrleansSearchCore()` method. Instead, import the `.Generated`
> namespace from your state assembly (e.g., `YourNamespace.Models.Generated`). The source generator creates
> an `AddOrleansSearch()` method there that properly registers all your search providers.

### 4. Query Your Grains

```csharp
// Get the cluster client
var client = serviceProvider.GetRequiredService<IClusterClient>();

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
│      IClusterClient.Search<TGrain>().Where(...).ToListAsync()    │
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
│                   Database Search Provider                       │
│            (PostgreSQL, SQL Server, etc.)                        │
├─────────────────────────────────────────────────────────────────┤
│                         Database                                 │
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

## Documentation

Full documentation is available at: **https://tyler-harker.github.io/TGHarker.Orleans.Search/**

## Samples

See the [Samples](./Samples/) folder for complete working examples including:

- User management with searchable properties
- E-commerce products with price ranges
- Order tracking with status filtering

## Claude Code Integration

If you're using [Claude Code](https://claude.ai/code), you can get interactive help implementing search for your Orleans grains:

```
/orleans-search
```

This skill provides step-by-step guidance on attributes, silo configuration, and query patterns.

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

## Troubleshooting

### "No search provider registered for grain type..."

This error means the search providers weren't registered. **The most common cause** is importing the wrong `AddOrleansSearch()` method.

**Wrong:**
```csharp
using TGHarker.Orleans.Search.Orleans.Extensions;  // ❌ Don't use this!
services.AddOrleansSearch();  // This only registers core services, not your providers
```

**Correct:**
```csharp
using YourNamespace.Models.Generated;  // ✅ Use your state namespace + .Generated
services.AddOrleansSearch();  // This registers core services AND all your search providers
```

The source generator creates an `AddOrleansSearch()` extension method in your state assembly's `.Generated` namespace. This method:
1. Calls the internal `AddOrleansSearchCore()` to set up the search infrastructure
2. Registers all your generated search providers (e.g., `AddUserSearch()`, `AddTenantSearch()`)

### For Aspire Users

When using .NET Aspire, you typically use `AddNpgsqlDbContext` instead of `UsePostgreSql`:

```csharp
using YourNamespace.Models.Generated;

// Aspire handles the connection string automatically
builder.AddNpgsqlDbContext<PostgreSqlSearchContext>("searchdb");
builder.Services.AddOrleansSearch();  // No need for .UsePostgreSql()
```

## Known Limitations

- All `[Searchable]` states in an assembly must be in the same namespace
- Only string-keyed grains (`IGrainWithStringKey`) are currently supported
- Sync errors are logged but don't fail the main storage operation

## License

MIT

## Contributing

Contributions are welcome! Please open an issue to discuss proposed changes.
