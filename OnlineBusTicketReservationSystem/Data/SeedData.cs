using Microsoft.AspNetCore.Identity;
using OnlineBusTicketReservationSystem.Models;

namespace OnlineBusTicketReservationSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            // Roles banao
            foreach (var role in new[] { "Admin", "Employee", "Customer" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Admin user banao
            if (await userManager.FindByEmailAsync("admin@srctravel.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@srctravel.com",
                    Email = "admin@srctravel.com",
                    FullName = "System Admin",
                    Role = "Admin",
                    Location = "Head Office",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Employee user banao
            if (await userManager.FindByEmailAsync("employee@srctravel.com") == null)
            {
                var emp = new ApplicationUser
                {
                    UserName = "employee@srctravel.com",
                    Email = "employee@srctravel.com",
                    FullName = "John Doe",
                    Role = "Employee",
                    EmployeeCode = "100001",
                    Age = 28,
                    Gender = "Male",
                    Address = "Branch Office",
                    Location = "Karachi",
                    Qualification = "Graduate",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(emp, "Employee@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(emp, "Employee");
            }
        }
    }
}