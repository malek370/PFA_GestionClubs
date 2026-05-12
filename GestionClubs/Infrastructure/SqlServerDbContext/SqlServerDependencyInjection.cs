using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Infrastructure.SqlServerDbContext
{
    public static class SqlServerDependencyInjection
    {
        public static void AddInfrastructureServices_SqlServer(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
            services.AddScoped<IBaseRepository<Club>, Repositories.ClubRepository>();
            services.AddScoped<IBaseRepository<Member>, Repositories.MemberRepository>();
            services.AddScoped<IBaseRepository<Adhesion>, Repositories.AdhesionRepository>();
            services.AddScoped<IBaseRepository<User>, Repositories.UserRepository>();
            services.AddScoped<IBaseRepository<Annoucement>, Repositories.AnnoucementRepository>();
            services.AddScoped<IBaseRepository<Event>, Repositories.EventRepository>();
        }
    }
}
