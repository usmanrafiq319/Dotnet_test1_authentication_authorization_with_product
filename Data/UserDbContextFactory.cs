using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();

            // Hardcode your development database string explicitly for the CLI tool
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=authwithproduct;Trusted_Connection=true;TrustServerCertificate=true");

            return new UserDbContext(optionsBuilder.Options);
        }
    }
}
