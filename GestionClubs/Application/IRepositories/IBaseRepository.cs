using GestionClubs.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Application.IRepositories
{
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        Task<IEnumerable<TEntity>> GetAll();
        Task<TEntity?> GetById(int id);
        Task<TEntity> Add(TEntity entity);
        Task<TEntity> Update(TEntity entity);
        Task<bool> Delete(int id);
        IQueryable<TEntity> GetAllQueryable();
    }
}
