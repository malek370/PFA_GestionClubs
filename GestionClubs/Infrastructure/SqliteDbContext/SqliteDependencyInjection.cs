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
        }

        public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();

            await db.Database.EnsureCreatedAsync();
            var listclubs = await db.Clubs.ToListAsync();

            if (db.Clubs.Any())
                return;

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "data.json");
            if (!File.Exists(jsonPath))
                jsonPath = Path.Combine(Path.GetDirectoryName(typeof(SqliteDependencyInjection).Assembly.Location)!, "data.json");
            var fileExist = File.Exists(jsonPath);
            if (!fileExist)
                return;

            var json = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var root = JsonSerializer.Deserialize<SeedDataRoot>(json, options);
            if (root?.Clubs == null)
                return;

            var clubs = root.Clubs;

            foreach (var c in clubs)
            {
                var club = new Club { Name = c.Name, Description = c.Description, Documents = c.Documents };
                foreach (var m in c.Members)
                    club.Members.Add(new Member { FirstName = m.FirstName, LastName = m.LastName, Email = m.Email, PostInClub = m.PostInClub });
                foreach (var a in c.Adhesions)
                    club.Adhesions.Add(new Adhesion { FirstName = a.FirstName, LastName = a.LastName, ClubId = a.ClubId, Email = a.Email, Status = a.Status });
                db.Clubs.Add(club);
            }

            await db.SaveChangesAsync();
        }

        private sealed class SeedDataRoot
        {
            public List<ClubSeedData> Clubs { get; set; } = [];
        }

        private sealed class ClubSeedData
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public System.Collections.ObjectModel.Collection<string> Documents { get; set; } = [];
            public List<MemberSeedData> Members { get; set; } = [];
            public List<AdhesionSeedData> Adhesions { get; set; } = [];
        }

        private sealed class MemberSeedData
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public ClubPost PostInClub { get; set; }
        }

        private sealed class AdhesionSeedData
        {
            public int ClubId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public Status Status { get; set; }
        }
    }
}
