using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MusicRec.Services.Identity.Api.Data;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        const string connectionString = "Server=.\\SQLEXPRESS;Database=MusicRecIdentityDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
