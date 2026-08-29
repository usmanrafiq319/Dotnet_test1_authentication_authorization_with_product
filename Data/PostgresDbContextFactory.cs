using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class PostgresDbContextFactory
        : IDesignTimeDbContextFactory<PostgresDbContext>
    {
        public PostgresDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString("UserDatabase");

            var optionsBuilder =
                new DbContextOptionsBuilder<PostgresDbContext>();

            optionsBuilder.UseNpgsql(
                connectionString,
                postgresOptions =>
                {
                    postgresOptions.MigrationsAssembly(
                        typeof(PostgresDbContext)
                            .Assembly
                            .GetName()
                            .Name);
                });

            return new PostgresDbContext(
                optionsBuilder.Options);
        }
    }
}
