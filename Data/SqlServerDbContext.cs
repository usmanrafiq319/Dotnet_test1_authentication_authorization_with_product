using Microsoft.EntityFrameworkCore;

namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class SqlServerDbContext : UserDbContext
    {
        public SqlServerDbContext(
            DbContextOptions<SqlServerDbContext> options): base(options)
        {
        }
    }
}