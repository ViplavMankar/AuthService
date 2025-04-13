using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AuthService.Data;

public static class DataExtensions
{
    public async static Task CreateAdminUser(this WebApplication app)
    {
        // Logic to create an admin user in the database
        // This is just a placeholder for the actual implementation
        using (var scope = app.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByNameAsync("admin");
            if (user == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    RefreshToken = GenerateSecureRefreshToken(),
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7) // or however long you want it to last
                };

                await userManager.CreateAsync(newUser, "Admin@123");
            }
        }
        return;
    }

    public static async Task MigrateDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Database migration succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");
        }
    }

    private static string GenerateSecureRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
