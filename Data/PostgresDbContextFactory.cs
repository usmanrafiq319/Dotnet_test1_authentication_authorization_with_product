using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
    {
        public PostgresDbContext CreateDbContext(string[] args)
        {
  
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // 1. Make appsettings optional so it doesn't crash if missing
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                // 2. Target this specific class assembly to reliably find the UserSecretsId
                .AddUserSecrets<PostgresDbContextFactory>(optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("UserDatabase");
            // Temporary debug line
            Console.WriteLine($"[DEBUG CS]: {connectionString}");

 

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'UserDatabase' was not found.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<PostgresDbContext>();

            optionsBuilder.UseNpgsql(
                connectionString,
                postgresOptions =>
                {
                    postgresOptions.MigrationsAssembly(
                        typeof(PostgresDbContext).Assembly.GetName().Name);
                });

            return new PostgresDbContext(optionsBuilder.Options);
        }
    }
}