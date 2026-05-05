using Microsoft.EntityFrameworkCore;
using CoShop.Data;

namespace CoShop.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)           => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync()       => await _set.ToListAsync();
    public async Task AddAsync(T entity)                  => await _set.AddAsync(entity);
    public void Update(T entity)                          => _set.Update(entity);
    public void Remove(T entity)                          => _set.Remove(entity);
    public async Task SaveChangesAsync()                  => await _db.SaveChangesAsync();
}
