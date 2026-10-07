using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TransManagement.Application.Common.Security;
using TransManagement.Domain.Entities;
using TransManagement.Infrastructure.Identity;

namespace TransManagement.Infrastructure.Persistence;

public static class DevelopmentDatabaseInitializer
{
    public static async Task InitializeDevelopmentDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    var errors = string.Join(" ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not create role '{roleName}': {errors}");
                }
            }
        }

        var adminEmail = configuration["DevelopmentAdmin:Email"];
        var adminPassword = configuration["DevelopmentAdmin:Password"];
        var adminFullName = configuration["DevelopmentAdmin:FullName"] ?? "Development Administrator";

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail.Trim(),
                    Email = adminEmail.Trim(),
                    FullName = adminFullName.Trim(),
                    EmailConfirmed = true
                };

                var createAdminResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createAdminResult.Succeeded)
                {
                    var errors = string.Join(" ", createAdminResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not create development administrator: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            {
                var addRoleResult = await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(" ", addRoleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not assign the Admin role: {errors}");
                }
            }
        }

        if (!await dbContext.Customers.AnyAsync(cancellationToken))
        {
            dbContext.Customers.Add(
                new Customer("CUS-001", "Khách hàng mẫu", "0900000000", "customer@example.com"));
        }

        if (!await dbContext.Drivers.AnyAsync(cancellationToken))
        {
            dbContext.Drivers.Add(
                new Driver(
                    "DRV-001",
                    "Tài xế mẫu",
                    "0911111111",
                    "GPLX-001",
                    "C",
                    DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5))));
        }

        if (!await dbContext.Vehicles.AnyAsync(cancellationToken))
        {
            dbContext.Vehicles.Add(
                new Vehicle("51C-000.01", "Xe tải", 3_500, 18));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
