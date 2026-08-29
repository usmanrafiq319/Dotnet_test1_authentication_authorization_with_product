using Microsoft.EntityFrameworkCore;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class PostgresDbContext : UserDbContext
    {
        public PostgresDbContext(
            DbContextOptions<PostgresDbContext> options)
            : base(options)
        {
        }
    }
}
