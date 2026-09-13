using System.Net.Http.Headers;
using System.Net.Http.Json;
using Merito.Server.Data;
using Merito.Shared.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Merito.Server.Tests.Support;

/// <summary>The real API pipeline over a SQLite in-memory database shared by every request of one test.</summary>
public sealed class MeritoApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Migrate", "false");
        builder.UseSetting("ConnectionStrings:Merito", "Host=unused");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<MeritoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<MeritoDbContext>>();
            services.AddDbContext<MeritoDbContext>(o => o.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<MeritoDbContext>().Database.EnsureCreated();
        });
    }

    /// <summary>Registers a parent and returns a client that sends their bearer token.</summary>
    public async Task<HttpClient> RegisterParentAsync(string name)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(name, $"{name.ToLowerInvariant()}{Guid.NewGuid():N}@example.com", "secret1"));
        response.EnsureSuccessStatusCode();
        return Authorize(client, (await response.Content.ReadFromJsonAsync<TokenResponse>())!);
    }

    /// <summary>Signs in with a login or email and returns a client that sends the bearer token.</summary>
    public async Task<HttpClient> LoginAsync(string login, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(login, password));
        response.EnsureSuccessStatusCode();
        return Authorize(client, (await response.Content.ReadFromJsonAsync<TokenResponse>())!);
    }

    private static HttpClient Authorize(HttpClient client, TokenResponse tokens)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
