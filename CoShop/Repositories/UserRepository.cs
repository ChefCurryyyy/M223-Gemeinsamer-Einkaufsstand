using Microsoft.EntityFrameworkCore;
using CoShop.Data;
using CoShop.Models;

namespace CoShop.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _set.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _set.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _set.AnyAsync(u => u.Email == email.ToLower());

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _set.AnyAsync(u => u.Username == username);
}
