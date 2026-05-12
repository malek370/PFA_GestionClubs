using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using GestionClubs.Infrastructure.SqlServerDbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Infrastructure.SqlServerDbContext.Repositories
{
    public class AdhesionRepository(AppDbContext context) : IBaseRepository<Adhesion>
    {
        private readonly AppDbContext _context = context;
        public async Task<Adhesion> Add(Adhesion entity)
        {
            await _context.Adhesions.AddAsync(entity);
            await _context.SaveChangesAsync();
            entity.Club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == entity.ClubId);
            entity.User = await _context.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var adhesion = await _context.Adhesions.FindAsync(id);
            if (adhesion == null)
            {
                return false;
            }
            _context.Adhesions.Remove(adhesion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Adhesion>> GetAll()
        {
            return await _context.Adhesions.Include(adh => adh.User).ToListAsync();
        }

        public  IQueryable<Adhesion> GetAllQueryable()
        {
            return  _context.Adhesions.Include(adh => adh.User).AsQueryable();
        }

        public Task<Adhesion?> GetById(int id)
        {
            return _context.Adhesions.Include(adh => adh.User).FirstOrDefaultAsync(adh=> adh.Id == id);
        }

        public async Task<Adhesion> Update(Adhesion entity)
        {
            _context.Adhesions.Update(entity);
            await _context.SaveChangesAsync(); 
            entity.Club= await _context.Clubs.FirstOrDefaultAsync(c => c.Id == entity.ClubId);
            entity.User= await _context.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId);
            return entity;
        }
    }
}
