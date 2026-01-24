using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Samples.Abstractions.ECommerce;
using Samples.Abstractions.Users;
using Samples.Grains.Generated;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Orleans.Search.Orleans;
using TGHarker.Orleans.Search.PostgreSQL.Extensions;

// Build the host with Orleans client
var builder = Host.CreateApplicationBuilder(args);

// Get PostgreSQL connection string (same as silo)
var connectionString = builder.Configuration["ConnectionStrings:SearchDb"]
    ?? "Host=localhost;Database=orleans_search_sample;Username=postgres;Password=postgres";

// Configure Orleans client
builder.UseOrleansClient(clientBuilder =>
{
    clientBuilder.UseLocalhostClustering();
});

// Add Orleans search with PostgreSQL provider (generated extension method)
builder.Services.AddOrleansSearch()
    .UsePostgreSql(connectionString);

var host = builder.Build();
await host.StartAsync();

// Get the searchable cluster client
var clusterClient = host.Services.GetRequiredService<ISearchableClusterClient>();

Console.WriteLine("Orleans Search Sample Client");
Console.WriteLine("============================\n");

// ===== USER EXAMPLES =====
Console.WriteLine("--- User Examples ---\n");

// Create some users
Console.WriteLine("Creating users...");
var userIds = new[] { "user-1", "user-2", "user-3", "user-4", "user-5" };
var users = new (string Email, string DisplayName, bool IsActive)[]
{
    ("alice@example.com", "Alice Johnson", true),
    ("bob@example.com", "Bob Smith", true),
    ("charlie@test.org", "Charlie Brown", false),
    ("diana@example.com", "Diana Prince", true),
    ("eve@test.org", "Eve Wilson", false)
};

for (int i = 0; i < userIds.Length; i++)
{
    var grain = clusterClient.GetGrain<IUserGrain>(userIds[i]);
    await grain.SetInfoAsync(users[i].Email, users[i].DisplayName, users[i].IsActive);
    Console.WriteLine($"  Created user: {users[i].DisplayName} ({users[i].Email})");
}

// Wait a moment for search index to sync
await Task.Delay(500);

// Search by exact email
// Note: The lambda parameter 'u' is typed as the generated UserSearch model
Console.WriteLine("\nSearching for user with email 'alice@example.com'...");
var aliceResult = await clusterClient.Search<IUserGrain>()
    .Where(u => u.Email == "alice@example.com")
    .FirstOrDefaultAsync();

if (aliceResult != null)
{
    var aliceInfo = await aliceResult.GetInfoAsync();
    Console.WriteLine($"  Found: {aliceInfo.DisplayName}");
}

// Search with Contains
Console.WriteLine("\nSearching for users with '@example.com' in email...");
var exampleUsers = await clusterClient.Search<IUserGrain>()
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();

Console.WriteLine($"  Found {exampleUsers.Count} users:");
foreach (var user in exampleUsers)
{
    var info = await user.GetInfoAsync();
    Console.WriteLine($"    - {info.DisplayName} ({info.Email})");
}

// Filter by IsActive
Console.WriteLine("\nSearching for active users...");
var activeUsers = await clusterClient.Search<IUserGrain>()
    .Where(u => u.IsActive == true)
    .ToListAsync();

Console.WriteLine($"  Found {activeUsers.Count} active users:");
foreach (var user in activeUsers)
{
    var info = await user.GetInfoAsync();
    Console.WriteLine($"    - {info.DisplayName}");
}

// Count inactive users
Console.WriteLine("\nCounting inactive users...");
var inactiveCount = await clusterClient.Search<IUserGrain>()
    .Where(u => u.IsActive == false)
    .CountAsync();
Console.WriteLine($"  Inactive users: {inactiveCount}");

// ===== E-COMMERCE EXAMPLES =====
Console.WriteLine("\n--- E-Commerce Examples ---\n");

// Create some products
Console.WriteLine("Creating products...");
var products = new (string Id, string Name, string Category, decimal Price, bool InStock)[]
{
    ("prod-1", "Laptop Pro 15", "Electronics", 1299.99m, true),
    ("prod-2", "Wireless Mouse", "Electronics", 29.99m, true),
    ("prod-3", "Office Chair", "Furniture", 249.99m, true),
    ("prod-4", "Standing Desk", "Furniture", 599.99m, false),
    ("prod-5", "USB-C Hub", "Electronics", 49.99m, true),
    ("prod-6", "Bookshelf", "Furniture", 149.99m, true),
    ("prod-7", "4K Monitor", "Electronics", 399.99m, false)
};

foreach (var product in products)
{
    var grain = clusterClient.GetGrain<IProductGrain>(product.Id);
    await grain.SetDetailsAsync(product.Name, product.Category, product.Price, product.InStock);
    Console.WriteLine($"  Created product: {product.Name} (${product.Price})");
}

await Task.Delay(500);

// Search products by category
Console.WriteLine("\nSearching for Electronics products...");
var electronics = await clusterClient.Search<IProductGrain>()
    .Where(p => p.Category == "Electronics")
    .ToListAsync();

Console.WriteLine($"  Found {electronics.Count} electronics products:");
foreach (var product in electronics)
{
    var info = await product.GetDetailsAsync();
    Console.WriteLine($"    - {info.Name}: ${info.Price} (In Stock: {info.InStock})");
}

// Search products by price range
Console.WriteLine("\nSearching for products between $100 and $500...");
var midRangeProducts = await clusterClient.Search<IProductGrain>()
    .Where(p => p.Price >= 100 && p.Price <= 500)
    .ToListAsync();

Console.WriteLine($"  Found {midRangeProducts.Count} products:");
foreach (var product in midRangeProducts)
{
    var info = await product.GetDetailsAsync();
    Console.WriteLine($"    - {info.Name}: ${info.Price}");
}

// Search for in-stock products only
Console.WriteLine("\nSearching for in-stock products...");
var inStockProducts = await clusterClient.Search<IProductGrain>()
    .Where(p => p.InStock == true)
    .ToListAsync();

Console.WriteLine($"  Found {inStockProducts.Count} in-stock products");

// Create some orders
Console.WriteLine("\nCreating orders...");
var orders = new (string Id, string UserId, string ProductId, int Quantity, string Status)[]
{
    ("order-1", "user-1", "prod-1", 1, "Completed"),
    ("order-2", "user-1", "prod-2", 2, "Completed"),
    ("order-3", "user-2", "prod-3", 1, "Shipped"),
    ("order-4", "user-3", "prod-5", 3, "Processing"),
    ("order-5", "user-4", "prod-6", 1, "Pending")
};

foreach (var order in orders)
{
    var grain = clusterClient.GetGrain<IOrderGrain>(order.Id);
    await grain.CreateOrderAsync(order.UserId, order.ProductId, order.Quantity, order.Status);
    Console.WriteLine($"  Created order: {order.Id} ({order.Status})");
}

await Task.Delay(500);

// Search orders by status
Console.WriteLine("\nSearching for completed orders...");
var completedOrders = await clusterClient.Search<IOrderGrain>()
    .Where(o => o.Status == "Completed")
    .ToListAsync();

Console.WriteLine($"  Found {completedOrders.Count} completed orders:");
foreach (var order in completedOrders)
{
    var info = await order.GetDetailsAsync();
    Console.WriteLine($"    - Order for User {info.UserId}, Product {info.ProductId}, Qty: {info.Quantity}");
}

// Search orders by user
Console.WriteLine("\nSearching for orders by user-1...");
var user1Orders = await clusterClient.Search<IOrderGrain>()
    .Where(o => o.UserId == "user-1")
    .ToListAsync();

Console.WriteLine($"  Found {user1Orders.Count} orders for user-1");

// Check if any pending orders exist
Console.WriteLine("\nChecking if any pending orders exist...");
var hasPending = await clusterClient.Search<IOrderGrain>()
    .Where(o => o.Status == "Pending")
    .AnyAsync();
Console.WriteLine($"  Has pending orders: {hasPending}");

Console.WriteLine("\n============================");
Console.WriteLine("Sample completed successfully!");

await host.StopAsync();
