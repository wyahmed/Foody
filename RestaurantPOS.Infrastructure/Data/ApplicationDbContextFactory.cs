using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RestaurantPOS.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core tooling (migrations, scaffolding).
/// Uses a local connection string so the full app doesn't need to start.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../RestaurantPOS.Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=RestaurantPOS;User Id=sa;******;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(130));

        return new ApplicationDbContext(optionsBuilder.Options, null!);
    }
}
