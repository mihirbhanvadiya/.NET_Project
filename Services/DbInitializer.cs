using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace OnlineJobAssignment.Services
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "JobProvider", "JobSeeker" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
