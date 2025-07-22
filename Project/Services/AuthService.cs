using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models.Entity;

namespace Project.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<bool> IsUsernameAvailableAsync(string username);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthService(ApplicationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.User
                .Include(u => u.Role)
                .Include(u => u.Title)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            if (_passwordService.VerifyPassword(password, user.PasswordHash))
                return user;

            return null;
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await _context.User.AnyAsync(u => u.Username == username);
        }
    }
}
