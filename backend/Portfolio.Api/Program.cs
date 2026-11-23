using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Api.Data;
using Portfolio.Api.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Portfolio.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var connectionString = builder.Configuration.GetConnectionString("Portfolio")
    ?? throw new InvalidOperationException("Connection string 'Portfolio' is not configured.");

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<PortfolioDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Portfolio.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var claimsIdentity = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<IdentityOptions>>().Value.ClaimsIdentity;
        var userId = context.Principal?.FindFirstValue(claimsIdentity.UserIdClaimType);
        var cookieStamp = context.Principal?.FindFirstValue(claimsIdentity.SecurityStampClaimType);
        var user = userId is null ? null : await userManager.FindByIdAsync(userId);
        var currentStamp = user is null ? null : await userManager.GetSecurityStampAsync(user);

        if (user is null || cookieStamp is null || !string.Equals(cookieStamp, currentStamp, StringComparison.Ordinal))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddAuthorization(options =>
    options.AddPolicy(PortfolioWritePolicy.Name, policy => policy.RequireRole("Editor", "Admin")));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IBlogpostService, BlogpostService>();
builder.Services.AddScoped<RoleAdministrationService>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var databaseScope = app.Services.CreateAsyncScope();
    var dbContext = databaseScope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    await dbContext.Database.MigrateAsync();
}

await IdentityRoleInitializer.InitializeAsync(app.Services);

var roleEmail = builder.Configuration["role-email"];
var roleName = builder.Configuration["role-name"];
if (roleEmail is not null || roleName is not null)
{
    if (string.IsNullOrWhiteSpace(roleEmail) || string.IsNullOrWhiteSpace(roleName))
    {
        throw new InvalidOperationException("Both --role-email and --role-name are required.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<RoleAdministrationService>()
        .SetRoleAsync(roleEmail, roleName);
    Console.WriteLine("Role updated successfully.");
    return;
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var isUnsafeMethod = method is not ("GET" or "HEAD" or "OPTIONS" or "TRACE");
    if (isUnsafeMethod && context.User.Identity?.IsAuthenticated == true)
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A valid anti-forgery token is required."
            });
            return;
        }
    }

    await next();
});

app.MapControllers();

app.Run();

public partial class Program;
