# TGHarker.Orleans.Search Samples

This folder contains sample projects demonstrating how to use the TGHarker.Orleans.Search library to add searchable grain capabilities to your Orleans applications.

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL database (for the search index)

## Project Structure

```
Samples/
├── Samples.Abstractions/     # Shared grain interfaces and DTOs
├── Samples.Grains/           # Grain implementations with searchable states
├── Samples.Silo/             # Orleans silo host
└── Samples.Client/           # Client demonstrating search queries
```

## Quick Start

### 1. Set Up PostgreSQL

Create a database for the search index:

```sql
CREATE DATABASE orleans_search_sample;
```

### 2. Configure Connection String (Optional)

By default, the samples use:
```
Host=localhost;Database=orleans_search_sample;Username=postgres;Password=postgres
```

To override, set the `ConnectionStrings:SearchDb` configuration or modify the default in `Program.cs`.

### 3. Run the Silo

```bash
cd Samples
dotnet run --project Samples.Silo
```

You should see:
```
Starting Orleans Silo...
Using PostgreSQL: Host=localhost...
```

### 4. Run the Client (in a separate terminal)

```bash
cd Samples
dotnet run --project Samples.Client
```

The client will:
1. Create sample users, products, and orders
2. Demonstrate various search queries
3. Display the results

## Key Concept: Selective Indexing

**You don't need to mark every property as `[Queryable]`** - only the ones you want to search on. This keeps your search index lean and performant.

```csharp
[Searchable(typeof(IUserGrain))]
public class UserState
{
    [Queryable]           // ✅ Can search by email
    public string Email { get; set; }

    [Queryable]           // ✅ Can search by name
    public string DisplayName { get; set; }

    public string Notes { get; set; }        // ❌ Stored but NOT searchable
    public byte[] Avatar { get; set; }       // ❌ Stored but NOT searchable
}
```

> **Best Practice**: Only index properties you actually need to query. Large text fields, binary data, or rarely-queried fields should be left without `[Queryable]`.

## Sample Domains

### Users

Demonstrates basic searchable properties:

```csharp
[Searchable(typeof(IUserGrain))]
[GenerateSerializer]
public class UserState
{
    [Queryable]                              // Indexed for email lookups
    public string Email { get; set; }

    [Queryable]                              // Indexed for name search
    [FullTextSearchable(Weight = 2.0)]       // + full-text search
    public string DisplayName { get; set; }

    [Queryable]
    public bool IsActive { get; set; }
}
```

### Products (E-Commerce)

Demonstrates indexed properties and numeric ranges:

```csharp
[Searchable(typeof(IProductGrain))]
[GenerateSerializer]
public class ProductState
{
    [Queryable]
    [FullTextSearchable(Weight = 2.0)]
    public string Name { get; set; }

    [Queryable(Indexed = true)]  // Creates database index
    public string Category { get; set; }

    [Queryable]
    public decimal Price { get; set; }

    [Queryable]
    public bool InStock { get; set; }
}
```

### Orders (E-Commerce)

Demonstrates DateTime queries and status filtering:

```csharp
[Searchable(typeof(IOrderGrain))]
[GenerateSerializer]
public class OrderState
{
    [Queryable]
    public string UserId { get; set; }

    [Queryable]
    public string ProductId { get; set; }

    [Queryable(Indexed = true)]
    public string Status { get; set; }

    [Queryable]
    public DateTime CreatedAt { get; set; }
}
```

## Search Query Examples

### Exact Match

```csharp
var user = await clusterClient.Search<IUserGrain>()
    .Where(u => u.Email == "alice@example.com")
    .FirstOrDefaultAsync();
```

### Contains (Partial Match)

```csharp
var users = await clusterClient.Search<IUserGrain>()
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();
```

### Boolean Filter

```csharp
var activeUsers = await clusterClient.Search<IUserGrain>()
    .Where(u => u.IsActive == true)
    .ToListAsync();
```

### Numeric Range

```csharp
var products = await clusterClient.Search<IProductGrain>()
    .Where(p => p.Price >= 100 && p.Price <= 500)
    .ToListAsync();
```

### Count

```csharp
var count = await clusterClient.Search<IUserGrain>()
    .Where(u => u.IsActive == false)
    .CountAsync();
```

### Any (Existence Check)

```csharp
var hasPending = await clusterClient.Search<IOrderGrain>()
    .Where(o => o.Status == "Pending")
    .AnyAsync();
```

## Configuration

### Silo Setup

```csharp
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    // Inner storage (your actual grain storage)
    siloBuilder.AddMemoryGrainStorage("InnerStorage");

    // Wrap with searchable storage
    siloBuilder.AddSearchableGrainStorage("InnerStorage");
});

// Add search services with PostgreSQL
builder.Services.AddOrleansSearch()
    .UsePostgreSql(connectionString);
```

### Client Setup

```csharp
builder.UseOrleansClient(clientBuilder =>
{
    clientBuilder.UseLocalhostClustering();
});

// Add search services (same as silo)
builder.Services.AddOrleansSearch()
    .UsePostgreSql(connectionString);

// Get the searchable client
var client = host.Services.GetRequiredService<ISearchableClusterClient>();
```

## Key Concepts

### Attributes

| Attribute | Purpose |
|-----------|---------|
| `[Searchable(typeof(IGrain))]` | Marks a state class as searchable, linking it to a grain interface |
| `[Queryable]` | **Opt-in**: Adds property to search index. Omit for properties you don't need to query. |
| `[Queryable(Indexed = true)]` | Creates a database index for faster queries on frequently filtered columns |
| `[FullTextSearchable]` | Enables full-text search on string properties |

**Remember**: Properties without `[Queryable]` are still stored in your grain - they just won't be in the search index.

### Generated Code

The source generator automatically creates:
- Entity classes for EF Core
- Search providers for each grain type
- Extension methods for DI registration
- LINQ-compatible `Where` methods

### How It Works

1. When grain state is written, `SearchableGrainStorage` intercepts and syncs to PostgreSQL
2. The search index stores grain IDs with queryable properties
3. LINQ queries are translated to EF Core and executed against PostgreSQL
4. Results are grain IDs, which are materialized into grain references

## Troubleshooting

### "No search provider registered"

Ensure you've called `AddOrleansSearch()` and the assembly containing your `[Searchable]` states is loaded.

### Queries return empty results

- Check that the silo is running and grains have been written
- Verify PostgreSQL connection
- Allow a brief delay after writes for index synchronization

### Build errors in generated code

All `[Searchable]` states must be in the same namespace due to a source generator limitation. Move states to a common namespace like `YourProject.Grains`.
