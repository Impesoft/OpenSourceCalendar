using CasaAdelia.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenSourceCalendar.Components;
using OpenSourceCalendar.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<BookingStateService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient();
// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();
// Register VisitorService
builder.Services.AddScoped<VisitorService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CONNECTIONSTRING") ?? throw new InvalidOperationException("Connection string 'CONNECTIONSTRING' not found.");
    options.UseSqlite(connectionString); //UseSqlServer(connectionString);
});

// Ensure the DbContextFactory is registered as a scoped service
builder.Services.AddScoped<IDbContextFactory<ApplicationDbContext>>(provider =>
{
    var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    return new PooledDbContextFactory<ApplicationDbContext>(options);
});
builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        Initialize(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying the database migrations.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OpenSourceCalendar.Client._Imports).Assembly);
app.MapHub<SignalRService>("/notificationhub");

app.Run();

static void Initialize(ApplicationDbContext context)
{
    // Ensure the database is created
    context.Database.EnsureCreated();

    // Check if there are any rooms already (avoid re-seeding if they exist)
    if (!context.Rooms.Any())
    {
        context.Rooms.AddRange(
            new Room { Name = "1eKamer" },
            new Room { Name = "2eKamer" },
            new Room { Name = "3eKamer" }
        );
        context.SaveChanges();
    }
}
