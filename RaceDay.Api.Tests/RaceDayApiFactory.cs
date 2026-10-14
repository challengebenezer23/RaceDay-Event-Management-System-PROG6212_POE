using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>
/// Boots the real RaceDay.Api application (via <see cref="Program"/>) for
/// integration testing, replacing the SQL Server <see cref="RaceDayDbContext"/>
/// registration with a fresh, isolated EF Core InMemory database per factory
/// instance. This is the pattern documented by Microsoft for testing with
/// WebApplicationFactory: https://learn.microsoft.com/aspnet/core/test/integration-tests
///
/// Every test class that needs its own clean database implements
/// IClassFixture&lt;RaceDayApiFactory&gt; and creates a new RaceDayApiFactory,
/// so tests never leak state into one another via a shared database name.
/// </summary>
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContextOptions registration added by
            // Program.cs so it doesn't try to reach a real database.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Build a scoped provider now so the in-memory database exists
            // and is schema-ready before the first request arrives.
            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            context.Database.EnsureCreated();
        });
    }
}
