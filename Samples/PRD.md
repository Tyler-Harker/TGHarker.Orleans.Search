# Product Requirements Document: TGHarker.Orleans.Search Samples

## Overview

### Purpose

The Samples project provides working examples demonstrating how to integrate TGHarker.Orleans.Search into Orleans applications. These samples serve as:

1. **Getting Started Guide** - Developers can clone and run immediately
2. **Reference Implementation** - Best practices for configuration and usage
3. **Test Bed** - Validate library functionality with realistic scenarios

### Target Audience

- .NET developers building Orleans-based distributed systems
- Teams needing to query grains by state properties
- Developers evaluating the library for adoption

## Requirements

### Functional Requirements

#### FR-1: Demonstrate Core Search Functionality

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1.1 | Show how to mark state classes as searchable using `[Searchable]` attribute | Must Have |
| FR-1.2 | Demonstrate `[Queryable]` attribute on various property types (string, bool, decimal, DateTime) | Must Have |
| FR-1.3 | Show `[Queryable(Indexed = true)]` for database-indexed properties | Should Have |
| FR-1.4 | Demonstrate `[FullTextSearchable]` for text search capabilities | Should Have |

#### FR-2: Demonstrate Search Query Patterns

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-2.1 | Exact match queries (`== value`) | Must Have |
| FR-2.2 | Contains queries for partial string matching | Must Have |
| FR-2.3 | Boolean filter queries | Must Have |
| FR-2.4 | Numeric range queries (`>=`, `<=`, `&&`) | Must Have |
| FR-2.5 | Count queries (`CountAsync`) | Should Have |
| FR-2.6 | Existence checks (`AnyAsync`) | Should Have |
| FR-2.7 | First/FirstOrDefault queries | Must Have |

#### FR-3: Demonstrate Configuration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-3.1 | Silo configuration with `AddSearchableGrainStorage` | Must Have |
| FR-3.2 | Client configuration with `AddOrleansSearch` | Must Have |
| FR-3.3 | PostgreSQL configuration with `UsePostgreSql` | Must Have |
| FR-3.4 | Connection string configuration via app settings | Should Have |

#### FR-4: Provide Realistic Domain Examples

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-4.1 | User management domain (email, name, active status) | Must Have |
| FR-4.2 | E-commerce domain (products with price, category, stock) | Must Have |
| FR-4.3 | Order management (status, user references, timestamps) | Should Have |

### Non-Functional Requirements

#### NFR-1: Usability

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-1.1 | Samples must build with single `dotnet build` command | Must Have |
| NFR-1.2 | Samples must run with minimal external dependencies (only PostgreSQL) | Must Have |
| NFR-1.3 | Code must be well-commented explaining key concepts | Should Have |
| NFR-1.4 | README must provide clear step-by-step instructions | Must Have |

#### NFR-2: Maintainability

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-2.1 | Use localhost clustering for simplicity | Must Have |
| NFR-2.2 | Use memory grain storage as inner storage | Should Have |
| NFR-2.3 | Separate projects for clear architectural boundaries | Must Have |

## Architecture

### Project Structure

```
Samples/
├── Samples.Abstractions/     # Grain interfaces (shared contract)
├── Samples.Grains/           # Implementation + source generator
├── Samples.Silo/             # Server-side host
└── Samples.Client/           # Consumer demonstrating queries
```

### Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client                                   │
│  1. Create grains via GetGrain<T>()                             │
│  2. Write state via grain methods                                │
│  3. Search via clusterClient.Search<T>().Where(...).ToListAsync()│
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                          Silo                                    │
│  - Hosts grain activations                                       │
│  - SearchableGrainStorage intercepts state writes               │
│  - Syncs to PostgreSQL search index                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       PostgreSQL                                 │
│  - Stores search index (grain ID + queryable properties)        │
│  - Executes LINQ queries via EF Core                            │
└─────────────────────────────────────────────────────────────────┘
```

## Domain Models

### User Domain

**Purpose**: Demonstrate basic searchable properties

| Property | Type | Searchable | Indexed | Full-Text |
|----------|------|------------|---------|-----------|
| Email | string | Yes | No | No |
| DisplayName | string | Yes | No | Yes (Weight: 2.0) |
| IsActive | bool | Yes | No | No |

**Use Cases**:
- Find user by email
- Search users by name
- Filter active/inactive users

### Product Domain

**Purpose**: Demonstrate numeric queries and categorization

| Property | Type | Searchable | Indexed | Full-Text |
|----------|------|------------|---------|-----------|
| Name | string | Yes | No | Yes (Weight: 2.0) |
| Category | string | Yes | Yes | No |
| Price | decimal | Yes | No | No |
| InStock | bool | Yes | No | No |

**Use Cases**:
- Find products by category
- Search products by price range
- Filter in-stock products

### Order Domain

**Purpose**: Demonstrate timestamps and status tracking

| Property | Type | Searchable | Indexed | Full-Text |
|----------|------|------------|---------|-----------|
| UserId | string | Yes | No | No |
| ProductId | string | Yes | No | No |
| Quantity | int | Yes | No | No |
| Status | string | Yes | Yes | No |
| CreatedAt | DateTime | Yes | No | No |

**Use Cases**:
- Find orders by status
- Find orders by user
- Check for pending orders

## Success Criteria

### Acceptance Criteria

1. **Build Success**: `dotnet build Samples/Samples.sln` completes with 0 errors
2. **Runtime Success**: Silo starts and accepts client connections
3. **Query Success**: All demonstrated queries return expected results
4. **Documentation**: README enables new developers to run samples in < 5 minutes

### Test Scenarios

| Scenario | Expected Outcome |
|----------|------------------|
| Create 5 users, search by email | Returns exact match |
| Create 5 users, search by email domain | Returns all matching users |
| Create 5 users, filter active | Returns only active users |
| Create 7 products, filter by category | Returns category matches |
| Create 7 products, filter by price range | Returns products in range |
| Create 5 orders, filter by status | Returns matching orders |
| Create 5 orders, check pending exists | Returns true |

## Constraints & Limitations

### Known Limitations

1. **Single Namespace Requirement**: All `[Searchable]` states must be in the same namespace due to source generator limitation
2. **PostgreSQL Only**: Samples only demonstrate PostgreSQL backend
3. **Localhost Clustering**: Production would use different clustering strategies
4. **Memory Storage**: Production would use persistent grain storage

### Out of Scope

- Production deployment configurations
- Authentication/authorization
- Performance benchmarking
- Multi-tenant scenarios
- Custom storage providers

## Future Enhancements

| Enhancement | Description | Priority |
|-------------|-------------|----------|
| Aspire Integration | Add .NET Aspire sample for orchestrated development | Medium |
| Azure Table Storage | Demonstrate with Azure backend | Low |
| Pagination Sample | Show Skip/Take patterns | Medium |
| Sorting Sample | Demonstrate OrderBy/OrderByDescending | Medium |
| Full-Text Search Demo | Dedicated sample for FTS capabilities | High |

## References

- [Microsoft Orleans Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL Full-Text Search](https://www.postgresql.org/docs/current/textsearch.html)
