# Product Requirements Document: TGHarker.Orleans.Search

## Executive Summary

TGHarker.Orleans.Search is a .NET library that bridges the gap between Orleans virtual actors and traditional database queries. It enables developers to query Orleans grains by their state properties without requiring manual index maintenance or custom infrastructure.

### Vision

Enable Orleans developers to find grains by "what they contain" rather than just "who they are" (their identity key), with zero additional code beyond attribute decoration.

### Problem Statement

Orleans grains are powerful for building distributed systems, but they present a fundamental challenge: **grains are accessed by identity, not by content**.

Common scenarios that are difficult without this library:

| Scenario | Traditional Orleans | With Orleans.Search |
|----------|---------------------|---------------------|
| Find user by email | Maintain separate lookup grain or external index | `Search<IUserGrain>().Where(u => u.Email == email)` |
| List all active users | Iterate all grains or maintain index grain | `Search<IUserGrain>().Where(u => u.IsActive)` |
| Find products under $100 | External database + manual sync | `Search<IProductGrain>().Where(p => p.Price < 100)` |

## Goals & Objectives

### Primary Goals

1. **Zero-Boilerplate Search** - Add search capability with only attribute decoration
2. **Automatic Synchronization** - Keep search index in sync with grain state automatically
3. **Familiar Query API** - Use LINQ expressions developers already know
4. **Production-Ready** - Leverage battle-tested PostgreSQL and EF Core

### Success Metrics

| Metric | Target |
|--------|--------|
| Lines of code to add search to existing grain | < 10 (attributes only) |
| Query latency overhead vs direct DB | < 50ms for simple queries |
| Index sync reliability | 99.9% (fire-and-forget with logging) |
| Supported property types | All primitive types + DateTime + Guid |

## Requirements

### Functional Requirements

#### FR-1: Attribute-Based Configuration

| ID | Requirement | Status |
|----|-------------|--------|
| FR-1.1 | `[Searchable(typeof(IGrain))]` marks state class as searchable | ✅ Implemented |
| FR-1.2 | `[Queryable]` marks individual properties for indexing | ✅ Implemented |
| FR-1.3 | `[Queryable(Indexed = true)]` creates database index | ✅ Implemented |
| FR-1.4 | `[FullTextSearchable]` enables PostgreSQL full-text search | ✅ Implemented |
| FR-1.5 | `[FullTextSearchable(Weight = n)]` sets relevance ranking | ✅ Implemented |

#### FR-2: Source Generation

| ID | Requirement | Status |
|----|-------------|--------|
| FR-2.1 | Generate EF Core entity classes from state classes | ✅ Implemented |
| FR-2.2 | Generate search providers with state-to-entity mapping | ✅ Implemented |
| FR-2.3 | Generate search model classes for type-safe queries | ✅ Implemented |
| FR-2.4 | Generate DI registration extension methods | ✅ Implemented |
| FR-2.5 | Generate LINQ `Where` extension methods per grain type | ✅ Implemented |

#### FR-3: Query Capabilities

| ID | Requirement | Status |
|----|-------------|--------|
| FR-3.1 | Equality queries (`== value`) | ✅ Implemented |
| FR-3.2 | String contains queries (`Contains`) | ✅ Implemented |
| FR-3.3 | Comparison queries (`>`, `<`, `>=`, `<=`) | ✅ Implemented |
| FR-3.4 | Boolean queries | ✅ Implemented |
| FR-3.5 | Combined queries with `&&` and `||` | ✅ Implemented |
| FR-3.6 | `ToListAsync()` - return all matches | ✅ Implemented |
| FR-3.7 | `FirstOrDefaultAsync()` - return first or null | ✅ Implemented |
| FR-3.8 | `FirstAsync()` - return first or throw | ✅ Implemented |
| FR-3.9 | `CountAsync()` - count matches | ✅ Implemented |
| FR-3.10 | `AnyAsync()` - check existence | ✅ Implemented |
| FR-3.11 | `OrderBy`/`OrderByDescending` | 🔲 Planned |
| FR-3.12 | `Skip`/`Take` pagination | 🔲 Planned |

#### FR-4: Storage Integration

| ID | Requirement | Status |
|----|-------------|--------|
| FR-4.1 | `SearchableGrainStorage` wraps existing storage providers | ✅ Implemented |
| FR-4.2 | Intercept `WriteStateAsync` to sync index | ✅ Implemented |
| FR-4.3 | Intercept `ClearStateAsync` to remove from index | ✅ Implemented |
| FR-4.4 | Optimistic concurrency with version checking | ✅ Implemented |
| FR-4.5 | Fire-and-forget sync (don't block main operation) | ✅ Implemented |

#### FR-5: Database Providers

| ID | Requirement | Status |
|----|-------------|--------|
| FR-5.1 | PostgreSQL via EF Core | ✅ Implemented |
| FR-5.2 | PostgreSQL full-text search integration | ✅ Implemented |
| FR-5.3 | SQL Server support | 🔲 Planned |
| FR-5.4 | Azure Cosmos DB support | 🔲 Planned |

### Non-Functional Requirements

#### NFR-1: Performance

| ID | Requirement | Status |
|----|-------------|--------|
| NFR-1.1 | Index sync must not block grain operations | ✅ Implemented |
| NFR-1.2 | Query execution via EF Core for optimization | ✅ Implemented |
| NFR-1.3 | Database indexes for queryable properties | ✅ Implemented |

#### NFR-2: Reliability

| ID | Requirement | Status |
|----|-------------|--------|
| NFR-2.1 | Sync failures logged but don't fail main operation | ✅ Implemented |
| NFR-2.2 | Version-based conflict resolution | ✅ Implemented |

#### NFR-3: Developer Experience

| ID | Requirement | Status |
|----|-------------|--------|
| NFR-3.1 | IntelliSense support for generated code | ✅ Implemented |
| NFR-3.2 | Compile-time errors for invalid configurations | ✅ Implemented |
| NFR-3.3 | XML documentation on generated code | ✅ Implemented |

## Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    TGHarker.Orleans.Search                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    Abstractions                              │   │
│  │  • ISearchEntity, ISearchModel<T>, ISearchProvider<T,S>     │   │
│  │  • [Searchable], [Queryable], [FullTextSearchable]          │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                      │
│  ┌───────────────────────────┴───────────────────────────────┐     │
│  │                         Core                               │     │
│  │  • SearchProviderBase<TGrain, TState, TEntity>            │     │
│  │  • OrleansQueryable<TGrain> / OrleansQueryProvider<T>     │     │
│  │  • OrleansQueryableExtensions (ToListAsync, etc.)         │     │
│  └───────────────────────────────────────────────────────────┘     │
│                              │                                      │
│  ┌───────────────────────────┴───────────────────────────────┐     │
│  │                        Orleans                             │     │
│  │  • SearchableGrainStorage (decorator pattern)             │     │
│  │  • ISearchableClusterClient / SearchableClusterClient     │     │
│  │  • ServiceCollection extensions                            │     │
│  └───────────────────────────────────────────────────────────┘     │
│                              │                                      │
│  ┌───────────────────────────┴───────────────────────────────┐     │
│  │                      PostgreSQL                            │     │
│  │  • PostgreSqlSearchContext (EF Core DbContext)            │     │
│  │  • UsePostgreSql() extension                              │     │
│  └───────────────────────────────────────────────────────────┘     │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                   SourceGenerator                           │   │
│  │  • EntityGenerator (Entity classes)                        │   │
│  │  • SearchProviderGenerator (Provider implementations)      │   │
│  │  • SearchModelGenerator (Query model classes)              │   │
│  │  • SearchClientExtensionsGenerator (Where extensions)      │   │
│  │  • DbContextGenerator (EF configurations)                  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Data Flow

#### Write Path

```
Grain.WriteStateAsync()
         │
         ▼
SearchableGrainStorage.WriteStateAsync()
         │
         ├──► InnerStorage.WriteStateAsync()  (actual persistence)
         │
         └──► SearchProvider.UpsertAsync()    (async, fire-and-forget)
                      │
                      ▼
              PostgreSqlSearchContext
                      │
                      ▼
               PostgreSQL DB
```

#### Query Path

```
client.Search<IGrain>().Where(predicate).ToListAsync()
         │
         ▼
GrainSearchExtensions.Where()  (generated)
         │
         ├──► TranslateSearchToEntity() (expression visitor)
         │
         ▼
OrleansQueryableExtensions.WhereEntity<TGrain, TEntity>()
         │
         ▼
OrleansQueryProvider.ExecuteAsync()
         │
         ├──► SearchProvider.QueryWithFilterAsync()
         │              │
         │              ▼
         │     PostgreSqlSearchContext.Set<TEntity>()
         │              │
         │              ▼
         │       EF Core → PostgreSQL
         │              │
         │              ▼
         │       List<string> grainIds
         │
         ▼
client.GetGrain<IGrain>(grainId) for each
         │
         ▼
List<IGrain> (grain references)
```

## API Reference

### Attributes

```csharp
// Mark state class as searchable
[Searchable(typeof(IMyGrain))]
public class MyState { }

// Mark property for search index
[Queryable]
public string Name { get; set; }

// Mark property with database index
[Queryable(Indexed = true)]
public string Category { get; set; }

// Enable full-text search
[FullTextSearchable]
public string Description { get; set; }

// Full-text with ranking weight
[FullTextSearchable(Weight = 2.0)]
public string Title { get; set; }
```

### Service Registration

```csharp
// Silo
builder.UseOrleans(silo =>
{
    silo.AddMemoryGrainStorage("Inner");
    silo.AddSearchableGrainStorage("Inner");  // Wraps "Inner"
});

builder.Services.AddOrleansSearch()  // Generated method
    .UsePostgreSql(connectionString);
```

### Query API

```csharp
var client = sp.GetRequiredService<ISearchableClusterClient>();

// Basic query
var results = await client.Search<IMyGrain>()
    .Where(x => x.Property == value)
    .ToListAsync();

// Combined conditions
var results = await client.Search<IProductGrain>()
    .Where(p => p.Price >= 100 && p.Price <= 500 && p.InStock)
    .ToListAsync();
```

## Constraints & Limitations

### Current Limitations

| Limitation | Workaround | Planned Fix |
|------------|------------|-------------|
| Single namespace for all searchable states | Use common namespace | Fix source generator |
| String-keyed grains only | Use string keys | Add Guid/long key support |
| No OrderBy/pagination | Post-filter in memory | Add to query provider |
| PostgreSQL only | Use PostgreSQL | Add SQL Server, Cosmos |

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| Fire-and-forget sync | Don't impact grain performance; search is eventually consistent |
| EF Core for queries | Leverage existing ORM infrastructure and optimizations |
| Source generation | Zero runtime reflection; compile-time safety |
| Decorator pattern for storage | Non-invasive; works with any existing storage provider |

## Roadmap

### v1.0 (Current)

- [x] Core attribute-based configuration
- [x] PostgreSQL support with EF Core
- [x] Basic LINQ queries (Where, First, Count, Any)
- [x] Full-text search support
- [x] Source generation for providers and extensions

### v1.1 (Planned)

- [ ] OrderBy/OrderByDescending support
- [ ] Skip/Take pagination
- [ ] Fix multi-namespace source generation
- [ ] Guid and long grain key support

### v2.0 (Future)

- [ ] SQL Server provider
- [ ] Azure Cosmos DB provider
- [ ] Aspire integration
- [ ] Real-time index updates via Orleans Streams
- [ ] Index migration tooling

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Orleans.Core | 10.0.0 | Orleans integration |
| Microsoft.Orleans.Runtime | 10.0.0 | Silo hosting |
| Microsoft.EntityFrameworkCore | 9.0.0+ | Database abstraction |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.0+ | PostgreSQL provider |

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Index out of sync | Low | Medium | Version checking, idempotent upserts |
| Query performance issues | Medium | Medium | Database indexes, EF Core query optimization |
| Source generator complexity | Medium | Low | Comprehensive test coverage |
| Breaking changes in Orleans | Low | High | Pin to specific Orleans version |

## Glossary

| Term | Definition |
|------|------------|
| Grain | Orleans virtual actor with identity and state |
| Search Index | PostgreSQL tables storing queryable grain properties |
| Search Provider | Generated class that handles CRUD for a grain type's index |
| Search Model | Generated class with properties matching queryable state |
| Searchable Storage | Decorator that intercepts writes and syncs to index |
