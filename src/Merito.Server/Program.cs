using Merito.Server.Data;
using Merito.Server.Features;
using Merito.Server.Features.Auth;
using Merito.Server.Features.Catalog;
using Merito.Server.Features.Families;
using Merito.Server.Features.Notifications;
using Merito.Server.Features.Points;
using Merito.Server.Features.Shop;
using Merito.Server.Features.Submissions;
using Merito.Server.Infrastructure;
using Merito.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MeritoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Merito")));

builder.Services.AddDataProtection()
    .SetApplicationName("Merito")
    .PersistKeysToDbContext<MeritoDbContext>();

// Bearer tokens only: no cookie scheme, so an unauthenticated API call answers 401 instead of a login redirect.
builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromHours(1);
        options.RefreshTokenExpiration = TimeSpan.FromDays(60);
    });
builder.Services.AddAuthorization();
builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.Password.RequiredLength = Limits.PasswordMinLength;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;
        options.Lockout.MaxFailedAccessAttempts = 10;
    })
    .AddEntityFrameworkStores<MeritoDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<FamilyAccess>();
builder.Services.AddScoped<FamilyService>();
builder.Services.AddScoped<ChildAccountService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<LedgerService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PointsService>();
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<ShopService>();

var app = builder.Build();

if (app.Configuration.GetValue("Database:Migrate", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MeritoDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapFamilyEndpoints();
app.MapCatalogEndpoints();
app.MapActivityEndpoints();
app.MapNotificationEndpoints();
app.Map("/api/{**rest}", () => Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Entry point, visible to the API tests' WebApplicationFactory.</summary>
public partial class Program;
