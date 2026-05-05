using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Infrastructure.SqliteDbContext
{
    public class SqliteDbContext(DbContextOptions<SqliteDbContext> options) : DbContext(options)
    {
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Adhesion> Adhesions { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Annoucement> Annoucements { get; set; }

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
