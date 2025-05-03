using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implementação genérica da interface IRepository<T>.
    /// Fornece a funcionalidade básica de acesso a dados usando Entity Framework Core.
    /// </summary>
    /// <typeparam name="T">O tipo da entidade, que deve herdar de BaseEntity.</typeparam>
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// Construtor que injeta o DbContext.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        public Repository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        /// <inheritdoc/>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> GetAllEnabledAsync()
        {
            return await _dbSet.Where(e => e.Enabled).ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task AddAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.CreatedAt = DateTime.UtcNow;
            entity.LastUpdate = DateTime.UtcNow;
            entity.Enabled = true;

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any()) throw new ArgumentNullException(nameof(entities));

            var now = DateTime.UtcNow;
            foreach (var entity in entities)
            {
                entity.CreatedAt = now;
                entity.LastUpdate = now;
                entity.Enabled = true;
            }

            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.LastUpdate = DateTime.UtcNow;

            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public virtual async Task RemoveAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public virtual async Task SoftRemoveAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.Enabled = false;
            entity.LastUpdate = DateTime.UtcNow;

            _dbSet.Attach(entity);
            _context.Entry(entity).Property(e => e.Enabled).IsModified = true;
            _context.Entry(entity).Property(e => e.LastUpdate).IsModified = true;

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public virtual async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any()) throw new ArgumentNullException(nameof(entities));

            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
