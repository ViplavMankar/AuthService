using AuthService.Models;
using Microsoft.AspNetCore.Identity;

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
                    Email = "admin@example.com"
                };
                await userManager.CreateAsync(newUser, "Admin@123"); // use a strong password
            }
        }
        return;
    }
}
