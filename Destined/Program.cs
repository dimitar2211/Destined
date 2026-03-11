using Destined.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    string[] roleNames = { "Admin", "User" };

    foreach (var role in roleNames)
    {
        var roleExists = await roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@site.com";
    var adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var newAdmin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(newAdmin, adminPassword);

        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, "Admin");
        }
    }

    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var usersWithoutCode = await dbContext.Users
        .Where(u => !dbContext.UserFriendCodes.Any(fc => fc.UserId == u.Id))
        .ToListAsync();

    if (usersWithoutCode.Any())
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        foreach (var user in usersWithoutCode)
        {
            string newCode;
            bool codeExists;
            do
            {
                newCode = new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                codeExists = dbContext.UserFriendCodes.Any(fc => fc.FriendCode == newCode);
            } while (codeExists);

            dbContext.UserFriendCodes.Add(new Destined.Models.UserFriendCode
            {
                UserId = user.Id,
                FriendCode = newCode
            });
            await dbContext.SaveChangesAsync();
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
