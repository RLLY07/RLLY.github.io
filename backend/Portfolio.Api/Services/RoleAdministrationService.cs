using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;

namespace Portfolio.Api.Services;

public class RoleAdministrationService(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    PortfolioDbContext dbContext)
{
    public async Task SetRoleAsync(string email, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException("The requested role does not exist.");
        }

        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("No account exists for the requested email.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && string.Equals(currentRoles[0], roleName, StringComparison.Ordinal))
        {
            return;
        }

        var rolesToRemove = currentRoles.Where(currentRole => currentRole != roleName).ToArray();
        if (rolesToRemove.Length > 0)
        {
            EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, rolesToRemove));
        }

        if (!currentRoles.Contains(roleName, StringComparer.Ordinal))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, roleName));
        }

        EnsureSucceeded(await userManager.UpdateSecurityStampAsync(user));
        await transaction.CommitAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Role update failed: {errors}");
        }
    }
}
