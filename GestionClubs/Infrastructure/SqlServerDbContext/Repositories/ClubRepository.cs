using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using GestionClubs.Infrastructure.SqlServerDbContext;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Infrastructure.SqlServerDbContext.Repositories
{
    public class ClubRepository(AppDbContext context) : IBaseRepository<Club>
    {
        private readonly AppDbContext _context = context;

        public async Task<Club> Add(Club entity)
        {
            await _context.Clubs.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null)
            {
                return false;
            }
            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Club>> GetAll()
        {
            return await _context.Clubs.ToListAsync();
        }

        public IQueryable<Club> GetAllQueryable()
        {
            return _context.Clubs.AsQueryable();
        }

        public Task<Club?> GetById(int id)
        {
            return _context.Clubs.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Club> Update(Club entity)
        {
            _context.Clubs.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
