using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QRShop.API.Data;
using QRShop.API.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// EF Core + MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    // An explicit server version avoids EF opening a connection at startup,
    // which would fail while the database container is still booting.
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)),
        mysql => mysql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Local file storage for images / certificates / QR codes.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Behind a reverse proxy (nginx), trust X-Forwarded-* so generated file URLs
// use the real public scheme/host instead of the container's internal address.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Docker network peers are not known ahead of time.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// CORS for the React dev server
const string CorsPolicy = "FrontendPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin =>
            {
                // Allow any localhost / 127.0.0.1 port in development.
                try { return new Uri(origin).Host is "localhost" or "127.0.0.1"; }
                catch { return false; }
            })
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Apply pending EF migrations on startup so a fresh container self-provisions
// its schema. Opt out with RUN_MIGRATIONS=false.
//
// On a host reboot the database container may not accept connections yet, so
// retry rather than crash — otherwise the API enters a restart loop.
if (app.Configuration.GetValue("RUN_MIGRATIONS", true))
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    const int maxAttempts = 20;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
            logger.LogInformation("Database migrations applied.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in 5s…",
                attempt, maxAttempts, ex.Message);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

// --- Pipeline ---
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // TLS is terminated by the reverse proxy in production, so only redirect locally.
    app.UseHttpsRedirection();
}

// Serve uploaded files from wwwroot/uploads as static URLs.
app.UseStaticFiles();

// Simple liveness probe for docker healthchecks.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
