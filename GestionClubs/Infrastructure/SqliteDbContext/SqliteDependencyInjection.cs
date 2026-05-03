using System.Text.Json;
using System.Text.Json.Serialization;
using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using GestionClubs.Infrastructure.SqliteDbContext.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace GestionClubs.Infrastructure.SqliteDbContext
{
    public static class SqliteDependencyInjection
    {
        public static void AddInfrastructureServices_Sqlite(this IServiceCollection services)
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "gestionclubs.db");
            dbPath = Path.GetFullPath(dbPath);
            services.AddDbContext<SqliteDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            services.AddScoped<IBaseRepository<Club>, ClubRepository>();
            services.AddScoped<IBaseRepository<Member>, MemberRepository>();
            services.AddScoped<IBaseRepository<Adhesion>, AdhesionRepository>();
            services.AddScoped<IBaseRepository<User>, UserRepository>();
        }

        public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();

            await db.Database.EnsureCreatedAsync();

            if (await db.Clubs.AnyAsync())
                return;

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "data.json");
            if (!File.Exists(jsonPath))
                jsonPath = Path.Combine(Path.GetDirectoryName(typeof(SqliteDependencyInjection).Assembly.Location)!, "data.json");
            if (!File.Exists(jsonPath))
                return;

            var json = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var root = JsonSerializer.Deserialize<SeedDataRoot>(json, options);
            if (root == null)
                return;

            if (root.Users != null)
            {
                foreach (var u in root.Users)
                    db.Users.Add(new User { Id = u.Id, Email = u.Email, FirstName = u.FirstName, LastName = u.LastName });
            }

            if (root.Clubs != null)
            {
                foreach (var c in root.Clubs)
                    db.Clubs.Add(new Club { Id = c.Id, Name = c.Name, Description = c.Description, Documents = c.Documents });
            }


            if (root.Members != null)
            {
                foreach (var m in root.Members)
                    db.Members.Add(new Member { Id = m.Id, ClubId = m.ClubId, UserId = m.UserId, PostInClub = m.PostInClub });
            }

            if (root.Adhesions != null)
            {
                foreach (var a in root.Adhesions)
                    db.Adhesions.Add(new Adhesion { Id = a.Id, ClubId = a.ClubId, UserId = a.UserId, Status = a.Status });
            }

            await db.SaveChangesAsync();
        }

        private sealed class SeedDataRoot
        {
            public List<UserSeedData>? Users { get; set; }
            public List<ClubSeedData>? Clubs { get; set; }
            public List<MemberSeedData>? Members { get; set; }
            public List<AdhesionSeedData>? Adhesions { get; set; }
        }

        private sealed class UserSeedData
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
        }

        private sealed class ClubSeedData
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public System.Collections.ObjectModel.Collection<string> Documents { get; set; } = [];
        }

        private sealed class MemberSeedData
        {
            public int Id { get; set; }
            public int ClubId { get; set; }
            [JsonPropertyName("usertId")]
            public int UserId { get; set; }
            public ClubPost PostInClub { get; set; }
        }

        private sealed class AdhesionSeedData
        {
            public int Id { get; set; }
            public int ClubId { get; set; }
            [JsonPropertyName("usertId")]
            public int UserId { get; set; }
            public Status Status { get; set; }
        }
    }
}
