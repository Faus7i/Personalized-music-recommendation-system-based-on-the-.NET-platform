using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MusicRec.Services.Identity.Api.Data;

namespace MusicRec.Tests;

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"IdentityTests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<IdentityDbContext>));
            services.RemoveAll(typeof(IdentityDbContext));

            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(databaseName, databaseRoot));
        });
    }
}
