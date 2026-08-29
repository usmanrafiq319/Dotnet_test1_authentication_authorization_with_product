using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class SqlServerDbContextFactory
        : IDesignTimeDbContextFactory<SqlServerDbContext>
    {
        public SqlServerDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile(
                    "appsettings.Development.json",
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString("UserDatabase");

            var optionsBuilder =
                new DbContextOptionsBuilder<SqlServerDbContext>();

            optionsBuilder.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(
                        typeof(SqlServerDbContext)
                            .Assembly
                            .GetName()
                            .Name);
                });

            return new SqlServerDbContext(
                optionsBuilder.Options);
        }
    }
}