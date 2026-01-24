using Microsoft.Extensions.Hosting;
using TGHarker.Orleans.Search.Orleans.Extensions;
using TGHarker.Orleans.Search.PostgreSQL.Extensions;
using Samples.Grains.Generated;

// Configure and start the Orleans silo
var builder = Host.CreateApplicationBuilder(args);

// Get PostgreSQL connection string from configuration or use default
var connectionString = builder.Configuration["ConnectionStrings:SearchDb"]
    ?? "Host=localhost;Database=orleans_search_sample;Username=postgres;Password=postgres";

builder.UseOrleans(siloBuilder =>
{
    // Use localhost clustering for development
    siloBuilder.UseLocalhostClustering();

    // Configure memory grain storage as the inner storage
    siloBuilder.AddMemoryGrainStorageAsDefault();
    siloBuilder.AddMemoryGrainStorage("InnerStorage");

    // Wrap with searchable storage that syncs to PostgreSQL
    siloBuilder.AddSearchableGrainStorage("InnerStorage");
});

// Add Orleans search with PostgreSQL provider
builder.Services.AddOrleansSearch()
    .UsePostgreSql(connectionString);

var host = builder.Build();

Console.WriteLine("Starting Orleans Silo...");
Console.WriteLine($"Using PostgreSQL: {connectionString.Split(';')[0]}...");

await host.RunAsync();
