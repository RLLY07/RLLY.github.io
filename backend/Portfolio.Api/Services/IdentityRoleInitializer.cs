using Microsoft.AspNetCore.Identity;

namespace Portfolio.Api.Services;

public static class IdentityRoleInitializer
{
    private static readonly string[] RoleNames = ["Gast", "Editor", "Admin"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in RoleNames)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Code));
                throw new InvalidOperationException($"Could not create Identity role '{roleName}': {errors}");
            }
        }
    }
}
