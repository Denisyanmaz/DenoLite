using JiraLite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JiraLite.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // unique DB name per factory instance (prevents cross-test-class leaking)
        public string DbName { get; } = $"JiraLite_TestDb_{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove existing registrations (from Program.cs)
                services.RemoveAll<DbContextOptions<JiraLiteDbContext>>();
                services.RemoveAll<JiraLiteDbContext>();

                // Register a single InMemory provider for this factory
                services.AddDbContext<JiraLiteDbContext>(options =>
                {
                    options.UseInMemoryDatabase(DbName);
                });

                // Ensure DB exists
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<JiraLiteDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}
