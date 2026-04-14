using IdentityProvider.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.DbContext
{
    public class IdpDbContext(DbContextOptions<IdpDbContext> options) : IdentityDbContext<User, IdentityRole<Guid>,Guid>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(256);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            });
        }
    }
}
