using GestionClubs.Application.IRepositories;
using GestionClubs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Infrastructure.SqlServerDbContext.Repositories
{
    public class MemberRepository(AppDbContext context) : IBaseRepository<Member>
    {
        private readonly AppDbContext _context = context;
        public async Task<Member> Add(Member entity)
        {
            await _context.Members.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return false;
            }
            _context.Members.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Member>> GetAll()
        {
            return await _context.Members.Include(m => m.User).ToListAsync();
        }

        public IQueryable<Member> GetAllQueryable()
        {
            return _context.Members.Include(m => m.User).AsQueryable();
        }

        public Task<Member?> GetById(int id)
        {
            return _context.Members.Include(m => m.Club).Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Member> Update(Member entity)
        {
            _context.Members.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}
