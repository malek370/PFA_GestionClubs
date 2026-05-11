using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Infrastructure.SqliteDbContext.Repositories
{
    public class EventRepository(SqliteDbContext context) : IBaseRepository<Event>
    {
        public async Task<Event> Add(Event entity)
        {
            await context.Events.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await context.Events.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            context.Events.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Event>> GetAll()
        {
            return await context.Events.ToListAsync();
        }

        public IQueryable<Event> GetAllQueryable()
        {
            return context.Events.Include(e=>e.Club);
        }

        public Task<Event?> GetById(int id)
        {
            return context.Events.Include(e=>e.Participent).Include(m => m.Club).FirstOrDefaultAsync(m => m.Id == id);

        }

        public async Task<Event> Update(Event entity)
        {
            context.Events.Update(entity);
            await context.SaveChangesAsync();
            return entity;
        }
    }
}
