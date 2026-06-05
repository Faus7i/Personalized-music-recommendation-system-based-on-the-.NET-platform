using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MusicRec.Services.Catalog.Api.Data;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        const string connectionString = "Server=.\\SQLEXPRESS;Database=MusicRecCatalogDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString);

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
