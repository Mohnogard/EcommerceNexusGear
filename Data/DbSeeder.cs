using Microsoft.AspNetCore.Identity;
using NexusGear.Models;

namespace NexusGear.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            foreach (var role in new[] { "Admin", "Customer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            if (await userManager.FindByEmailAsync("admin@nexusgear.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@nexusgear.com",
                    Email = "admin@nexusgear.com",
                    FirstName = "Admin",
                    LastName = "NexusGear",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
            }

           
        }
    }
}
