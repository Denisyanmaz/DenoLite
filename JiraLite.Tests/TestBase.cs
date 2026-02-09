using System.Net.Http;
using JiraLite.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JiraLite.Tests
{
    public abstract class TestBase : IClassFixture<JiraLite.Tests.Integration.CustomWebApplicationFactory>, IAsyncLifetime
    {
        protected readonly HttpClient Client;
        protected readonly JiraLiteDbContext Db;
        private readonly IServiceScope _scope;

        protected TestBase(JiraLite.Tests.Integration.CustomWebApplicationFactory factory)
        {
            Client = factory.CreateClient();

            _scope = factory.Services.CreateScope();
            Db = _scope.ServiceProvider.GetRequiredService<JiraLiteDbContext>();
        }

        public async Task InitializeAsync()
        {
            // Reset DB for EVERY test so tests are deterministic
            await Db.Database.EnsureDeletedAsync();
            await Db.Database.EnsureCreatedAsync();

            // Avoid auth header leaking between tests
            Client.DefaultRequestHeaders.Authorization = null;
        }

        public Task DisposeAsync()
        {
            _scope.Dispose();
            return Task.CompletedTask;
        }
    }
}
