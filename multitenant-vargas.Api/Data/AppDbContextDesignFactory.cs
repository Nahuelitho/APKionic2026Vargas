using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace multitenant_vargas.Api.Data;

// EF tooling usa la misma configuracion que la API sin arrancar el servidor.
public sealed class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        var connection = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("No se configuro ConnectionStrings:Default para EF.");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connection,
                new MySqlServerVersion(new Version(8, 0, 0)))
            .UseSnakeCaseNamingConvention().Options;
        return new AppDbContext(options);
    }
}
