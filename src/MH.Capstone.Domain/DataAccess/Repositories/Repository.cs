using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

// Code adapted from file give to students by Dr. Morgan in CS 460,
// Used with permission, and modified to add features and needs specific to this project.
namespace MH.Capstone.Domain.DataAccess.Repositories
{
    /// <summary>
    /// Base class repository that implements common and CRUD operations.  Meant to be like an abstract 
    /// base class, but not actually made abstract because it is often useful to have a repository 
    /// that only supports these common operations.
    /// 
    /// This is only a minimal version. There is quite a lot we could add to this, including:
    ///    - add better error checking/recovery (i.e. throw exceptions when something goes wrong; write a 
    ///      custom exception class to handle errors)
    ///    - Write a base model class for the parameterized type, i.e. require TEntity : ModelBase, 
    ///      and have ModelBase define important things like the PK name and type so we can enforce that in
    ///      FindById for example.
    ///    - Async versions
    /// </summary>
    /// <typeparam name="TEntity">This is the entity for which we're making a repository</typeparam>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, new()
    {
        // The context is private to enforce full separation, preventing a subclass from accessing
        // entities other than this one.  If you need other entities, write a separate "service" or "provider" class
        // that has access to all repositories needed
        private readonly DbContext _context;
        // This one is OK to use in a subclass because it only represents the entity for this type of repository
        // and it can be mocked
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(DbContext ctx)
        {
            _context = ctx;
            _dbSet = _context.Set<TEntity>();   // must do it this way because we don't have a "navigation property" to use
        }

        // Find by ID assuming it's the PK and is an int
        public virtual async Task<TEntity?> FindByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            return entity;  // null if not found
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await FindByIdAsync(id) != null;
        }

        public virtual async Task<IQueryable<TEntity>> GetAllAsync()
        {
            // note, no includes here, and we're returning it as an IQueryable, NOT a DbSet, on purpose
            // so the caller cannot do other things (which should go here or in a subclass)
            return await Task.FromResult(_dbSet);
        }

        public async Task<IQueryable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes)
        {
            // Apply includes one by one
            IQueryable<TEntity> dbs = _dbSet;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var item in includes)
            {
                dbs = dbs.Include(item);
            }

            return await Task.FromResult(dbs);
        }

        public async Task<IQueryable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate)
        {
            IQueryable<TEntity> dbs = _dbSet;
            return await Task.FromResult(dbs.Where(predicate));
        }

        public virtual async Task<TEntity> AddOrUpdateAsync(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity must not be null to add or update");
            }
            _context.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task DeleteAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteByIdAsync(int id)
        {
            // If the entity doesn't exist, FindById will return null
            // and Delete will throw an exception, which is what we want
            await DeleteAsync((await FindByIdAsync(id))!);
        }
    }

    /// <summary>
    /// Interface for common and CRUD operations on entities
    /// </summary>
    /// <typeparam name="TEntity">This is the entity for which we're making a repository</typeparam>
    public interface IRepository<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// Find entity by PK.  This only works for entities with integer PK's. 
        /// </summary>
        /// <param name="id">The PK of the entity to find</param>
        /// <returns>The entity or null if not found</returns>
        Task<TEntity?> FindByIdAsync(int id);

        /// <summary>
        /// Check if the entity with this integer PK exists in the table
        /// </summary>
        /// <param name="id">The PK of the entity to check</param>
        /// <returns>True if the entity exists, False otherwise</returns>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Get all entities in this table.  Note, when eager loading is used this
        /// method will NOT populate navigation properties associated with foreign keys.
        /// </summary>
        /// <returns>All the entities</returns>
        Task<IQueryable<TEntity>> GetAllAsync();

        /// <summary>
        /// Get all entities in this table that satisfy the given predicate.
        /// </summary>
        /// <param name="predicate">All the entities</param>
        /// <returns></returns>
        Task<IQueryable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Version of GetAll that will perform includes to load navigation properties, 
        /// but only first level ones.  It will NOT do ThenInclude.
        /// </summary>
        /// <param name="includes">Lambda functions that represent includes of properties</param>
        /// <returns>All Entities with all the includes</returns>
        Task<IQueryable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Add a new entity or update an existing one.  A new entity is one in which 
        /// the PK has the default value for that type, i.e. for integer PK this is 0.  This
        /// also assumes the PK is auto-generated in the table
        /// </summary>
        /// <param name="entity">The entity to add or update</param>
        /// <returns>The entity that was added or updated, suitably synced with the DB</returns>
        Task<TEntity> AddOrUpdateAsync(TEntity entity);

        /// <summary>
        /// Remove this entity from the DB.  If the entity is not in the DB or has not been
        /// previously added, it "should" do nothing (note: I haven't checked this yet)
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Remove the entity having this PK from the DB
        /// </summary>
        /// <param name="id">The integer PK of the entity to remove</param>
        /// <exception cref="System.Exception">Thrown if no entity with this PK id exists</exception>
        Task DeleteByIdAsync(int id);
    }
}