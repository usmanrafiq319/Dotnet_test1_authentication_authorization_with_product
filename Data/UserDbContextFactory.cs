using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            var connectionString = configuration.GetConnectionString("UserDatabase");
            var assemblyName = typeof(UserDbContext).Assembly.GetName().Name;

            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                optionsBuilder.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly(assemblyName)
                          .MigrationsHistoryTable("__EFMigrationsHistory")
                );
            }
            else
            {
                optionsBuilder.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(assemblyName)
                          .MigrationsHistoryTable("__EFMigrationsHistory")
                );
            }

            return new UserDbContext(optionsBuilder.Options);
        }
    }
}