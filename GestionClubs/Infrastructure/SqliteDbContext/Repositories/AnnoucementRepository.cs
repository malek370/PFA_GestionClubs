using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Infrastructure.SqliteDbContext.Repositories
{
    public class AnnoucementRepository(SqliteDbContext _context) : IBaseRepository<Annoucement>
    {
        public async Task<Annoucement> Add(Annoucement entity)
        {
            await _context.Annoucements.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _context.Annoucements.FindAsync(id);
            if (entity is null)
                return false;

            _context.Annoucements.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Annoucement>> GetAll()
        {
            return await _context.Annoucements.Include(ann=>ann.Club).ToListAsync();
        }

        public IQueryable<Annoucement> GetAllQueryable()
        {
            return _context.Annoucements.Include(ann=>ann.Club).AsQueryable();
        }

        public async Task<Annoucement?> GetById(int id)
        {
            return await _context.Annoucements.Include(ann=>ann.Club).FirstOrDefaultAsync(ann=>ann.Id == id);
        }

        public async Task<Annoucement> Update(Annoucement entity)
        {
            _context.Annoucements.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
