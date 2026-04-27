using IdentityProvider.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityProvider.DbContext
{
    public class IdpDbContextFactory : IDesignTimeDbContextFactory<IdpDbContext>
    {
        public IdpDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IdpDbContext>();
            optionsBuilder.UseSqlite("Data Source=mabase.db");

            return new IdpDbContext(optionsBuilder.Options);
        }
    }
}