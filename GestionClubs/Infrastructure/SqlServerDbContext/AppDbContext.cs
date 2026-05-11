using GestionClubs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Infrastructure.SqlServerDbContext
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Adhesion> Adhesions { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Annoucement> Annoucements { get; set; }
        public DbSet<Event> Events { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatinDate = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Club>().ToTable("Clubs");
            modelBuilder.Entity<Adhesion>().ToTable("Adhesions");
            modelBuilder.Entity<Member>().ToTable("Members");
            modelBuilder.Entity<User>().ToTable("Users");

        }
    }
}
