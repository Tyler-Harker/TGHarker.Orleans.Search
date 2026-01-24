# TGHarker.Orleans.Search - Implementation Guide

You are helping a developer implement search functionality for Orleans grains using TGHarker.Orleans.Search. This library enables querying grains by their state using LINQ, backed by PostgreSQL.

## Core Concepts

1. **[Searchable]** - Marks a grain state class for search indexing, linking it to a grain interface
2. **[Queryable]** - Opt-in: Only properties with this attribute are indexed (keep your index lean)
3. **[FullTextSearchable]** - Enables PostgreSQL full-text search on string properties
4. **SearchableGrainStorage** - Decorator that syncs state changes to the search database
5. **IClusterClient.Search<TGrain>()** - LINQ queryable for searching grains

## Step-by-Step Implementation

### Step 1: Install Packages

```bash
dotnet add package TGHarker.Orleans.Search
dotnet add package TGHarker.Orleans.Search.PostgreSQL
```

### Step 2: Create Searchable Grain State

Mark your grain state with `[Searchable]` and add `[Queryable]` only to properties you need to search:

```csharp
using Orleans;
using TGHarker.Orleans.Search.Abstractions.Attributes;

[Searchable(typeof(IUserGrain))]  // Link to grain interface
[GenerateSerializer]
public class UserState
{
    [Queryable]                              // Indexed - can search by email
    [Id(0)]
    public string Email { get; set; } = string.Empty;

    [Queryable]                              // Indexed - can search by name
    [FullTextSearchable(Weight = 2.0)]       // Also supports full-text search
    [Id(1)]
    public string DisplayName { get; set; } = string.Empty;

    [Queryable]                              // Indexed - can filter by status
    [Id(2)]
    public bool IsActive { get; set; }

    [Id(3)]                                  // NOT indexed - stored but not searchable
    public string InternalNotes { get; set; } = string.Empty;

    [Id(4)]                                  // NOT indexed - stored but not searchable
    public DateTime LastLoginUtc { get; set; }
}
```

**Important**: Only add `[Queryable]` to properties you actually need to query. Each indexed property adds database storage and sync overhead.

### Step 3: Configure the Silo

```csharp
using TGHarker.Orleans.Search.Orleans.Extensions;
using TGHarker.Orleans.Search.PostgreSQL.Extensions;
using YourNamespace.Generated;  // Generated code namespace

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    // Configure inner storage (your actual grain storage)
    siloBuilder.AddMemoryGrainStorage("InnerStorage");

    // Wrap with searchable storage (syncs to search DB on writes)
    siloBuilder.AddSearchableGrainStorage("InnerStorage");
});

// Add search services with PostgreSQL
var connectionString = "Host=localhost;Database=orleans_search;Username=postgres;Password=postgres";
builder.Services.AddOrleansSearch()
    .UsePostgreSql(connectionString);

await builder.Build().RunAsync();
```

### Step 4: Query Grains

```csharp
using TGHarker.Orleans.Search.Core.Extensions;
using YourNamespace.Generated;

public class UserService
{
    private readonly IClusterClient _client;

    public UserService(IClusterClient client)
    {
        _client = client;
    }

    public async Task<List<IUserGrain>> FindActiveUsersAsync()
    {
        return await _client.Search<IUserGrain>()
            .WhereEntity<IUserGrain, UserStateEntity>(e => e.IsActive)
            .ToListAsync();
    }

    public async Task<List<IUserGrain>> SearchByEmailAsync(string domain)
    {
        return await _client.Search<IUserGrain>()
            .WhereEntity<IUserGrain, UserStateEntity>(e => e.Email.Contains(domain))
            .ToListAsync();
    }

    public async Task<IUserGrain?> FindByEmailAsync(string email)
    {
        return await _client.Search<IUserGrain>()
            .WhereEntity<IUserGrain, UserStateEntity>(e => e.Email == email)
            .FirstOrDefaultAsync();
    }
}
```

## Attribute Reference

| Attribute | Usage | Description |
|-----------|-------|-------------|
| `[Searchable(typeof(IGrain))]` | Class | Required. Links state to grain interface |
| `[Queryable]` | Property | Opt-in indexing. Only marked properties are searchable |
| `[Queryable(Indexed = true)]` | Property | Creates database index for faster queries |
| `[FullTextSearchable]` | Property | Enables PostgreSQL full-text search |
| `[FullTextSearchable(Weight = 2.0)]` | Property | Boosts relevance in search results |

## Supported Property Types

- `string`, `bool`, `int`, `long`, `short`, `byte`
- `decimal`, `double`, `float`
- `DateTime`, `DateTimeOffset`, `Guid`

## Query Methods

| Method | Description |
|--------|-------------|
| `.WhereEntity<TGrain, TEntity>(predicate)` | Filter by entity properties |
| `.ToListAsync()` | Execute query, return list of grain references |
| `.FirstOrDefaultAsync()` | Return first match or null |
| `.FirstAsync()` | Return first match, throw if none |
| `.CountAsync()` | Return count of matches |
| `.AnyAsync()` | Check if any matches exist |

## Best Practices

1. **Index Selectively**: Only add `[Queryable]` to properties you actually query
2. **Use Database Indexes**: Add `[Queryable(Indexed = true)]` for high-cardinality columns
3. **Full-Text for Search**: Use `[FullTextSearchable]` for user-facing search features
4. **Keep States Small**: Large states slow sync operations
5. **Handle Errors Gracefully**: Search sync errors are logged but don't fail writes

## Generated Code

The source generator creates:
- `{StateName}Entity` - EF Core entity class
- `{StateName}SearchProvider` - Search provider implementation
- `{GrainName}Search` - Search model for fluent API
- `GeneratedSearchRegistration` - DI extension methods

## Common Patterns

### Pagination
```csharp
var page = await _client.Search<IProductGrain>()
    .WhereEntity<IProductGrain, ProductStateEntity>(e => e.Category == "Electronics")
    .Skip(20)
    .Take(10)
    .ToListAsync();
```

### Combined Filters
```csharp
var results = await _client.Search<IOrderGrain>()
    .WhereEntity<IOrderGrain, OrderStateEntity>(e =>
        e.Status == "Pending" &&
        e.CreatedAt > DateTime.UtcNow.AddDays(-7))
    .ToListAsync();
```

### Exists Check
```csharp
var emailExists = await _client.Search<IUserGrain>()
    .WhereEntity<IUserGrain, UserStateEntity>(e => e.Email == email)
    .AnyAsync();
```

When helping developers, always emphasize that `[Queryable]` is opt-in - they should only index what they need to query.
