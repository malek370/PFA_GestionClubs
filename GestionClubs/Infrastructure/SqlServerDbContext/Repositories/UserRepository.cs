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
    public class UserRepository(AppDbContext dbContext) : IBaseRepository<User>
    {
        public async Task<User> Add(User entity)
        {
            await dbContext.Users.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user != null)
            {
                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await dbContext.Users.ToListAsync();
        }

        public  IQueryable<User> GetAllQueryable()
        {
            return  dbContext.Users.AsQueryable();
        }

        public Task<User?> GetById(int id)
        {
            return dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> Update(User entity)
        {
            dbContext.Users.Update(entity);
            await dbContext.SaveChangesAsync(); 
            return entity;
        }
    }
}
