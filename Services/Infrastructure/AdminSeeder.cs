using E_commerce_Application.Configuration;
using E_commerce_Application.DbContext;
using E_commerce_Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace E_commerce_Application.Services.Infrastructure;

public class AdminSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly AdminUserSettings _settings;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        ApplicationDbContext context,
        IOptions<AdminUserSettings> settings,
        ILogger<AdminSeeder> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.Email) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning("Admin user seeding skipped because email or password is not configured.");
            return;
        }

        var existingAdmin = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == _settings.Email);

        if (existingAdmin is not null)
        {
            if (existingAdmin.Role != UserRole.Admin)
            {
                existingAdmin.Role = UserRole.Admin;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Existing user {Email} promoted to admin.", _settings.Email);
            }

            return;
        }

        var adminUser = new User
        {
            Name = string.IsNullOrWhiteSpace(_settings.Name) ? "Administrator" : _settings.Name,
            Email = _settings.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_settings.Password),
            Role = UserRole.Admin,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Default admin user {Email} created.", _settings.Email);
    }
}


