using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QRShop.API.Data;
using QRShop.API.Json;
using QRShop.API.Services;

// Load the repo-root .env before configuration is built, so `dotnet run` picks
// up the same file docker-compose uses. Real environment variables always win,
// so containers, CI and the server are unaffected by this.
LoadDotEnv(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers()
    // Whitespace is stripped off every incoming string before model validation
    // runs, so " " fails a [Required] instead of passing it.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new TrimmingStringConverter()));
builder.Services.AddOpenApi();

// [ApiController] returns a ValidationProblemDetails on a model-validation
// failure, but every hand-written error in this API is shaped { message },
// and that is what the React client reads. Reshape the automatic 400 to match
// while still exposing the per-field errors for inline display.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .ToDictionary(
                kv => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(kv.Key),
                kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var message = errors.Values.SelectMany(m => m).FirstOrDefault() ?? "Invalid request.";

        return new BadRequestObjectResult(new { message, errors });
    };
});

// EF Core + MySQL.
// docker-compose sets ConnectionStrings__DefaultConnection directly. For local
// runs it is absent, so build it from the DB_* values in .env — that keeps the
// password in exactly one place instead of duplicating it per environment.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    if (string.IsNullOrWhiteSpace(dbPassword))
    {
        throw new InvalidOperationException(
            "No database configuration found. Copy .env.example to .env in the repo root " +
            "and set DB_PASSWORD (or set ConnectionStrings__DefaultConnection directly).");
    }

    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "QRShopDb";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
    connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";
}
builder.Services.AddDbContext<AppDbContext>(options =>
    // An explicit server version avoids EF opening a connection at startup,
    // which would fail while the database container is still booting.
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)),
        mysql => mysql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// --- Authentication ---
// The React client signs in with Firebase and sends the resulting ID token as a
// bearer on every request. Verifying it needs nothing but the project id:
// Google publishes the signing keys at the authority below and the JwtBearer
// handler fetches and caches them, so there is no service-account JSON to ship.
var firebaseProjectId =
    builder.Configuration["FIREBASE_PROJECT_ID"]
    ?? builder.Configuration["VITE_FIREBASE_PROJECT_ID"];

if (string.IsNullOrWhiteSpace(firebaseProjectId))
{
    throw new InvalidOperationException(
        "FIREBASE_PROJECT_ID (or VITE_FIREBASE_PROJECT_ID) is not set. Add it to .env — " +
        "without it the API cannot verify the Firebase ID tokens the client sends.");
}

var firebaseIssuer = $"https://securetoken.google.com/{firebaseProjectId}";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = firebaseIssuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = firebaseIssuer,
            ValidateAudience = true,
            // Firebase sets aud to the bare project id, not the issuer URL.
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true,
            // Tokens live an hour; allow only a little drift between hosts.
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });
builder.Services.AddAuthorization();

// Local file storage for images / certificates / QR codes.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- Billing ---
// Typed client so the Razorpay HttpClient (base address + basic auth) is built
// once and its connections pooled.
builder.Services.AddHttpClient<IRazorpayService, RazorpayService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// AI product descriptions. One POST to an OpenAI-compatible endpoint — the key
// stays server-side, so it is never exposed in the frontend bundle.
builder.Services.AddHttpClient<IAiDescriptionService, AiDescriptionService>();

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

    // A QR image has its target URL drawn into the pixels, so changing
    // PUBLIC_BASE_URL leaves every existing code pointing at the old address.
    // Rewriting the stale ones here means deploying to a new domain or IP needs
    // no extra step — and no authenticated call a deploy script cannot make.
    try
    {
        using var scope = app.Services.CreateScope();
        var qr = scope.ServiceProvider.GetRequiredService<IQrCodeService>();
        var refreshed = await qr.RefreshStaleAsync();
        if (refreshed > 0)
            logger.LogInformation("Regenerated {Count} QR code(s) for {BaseUrl}.",
                refreshed, PublicUrls.Base(app.Configuration));
    }
    catch (Exception ex)
    {
        // Never block startup over this — the site works, the codes are just stale.
        logger.LogWarning("Could not refresh QR codes: {Message}", ex.Message);
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

// Walks up from the working directory looking for a .env, then sets any key it
// finds that is not already an environment variable. Deliberately does not
// overwrite: a value exported by the shell, docker-compose or the server's
// service manager must always take precedence over the file.
static void LoadDotEnv(string startDirectory)
{
    var dir = new DirectoryInfo(startDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".env")))
        dir = dir.Parent;

    if (dir is null) return;

    foreach (var rawLine in File.ReadAllLines(Path.Combine(dir.FullName, ".env")))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;

        var separator = line.IndexOf('=');
        if (separator <= 0) continue;

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim().Trim('"');

        if (Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }
}
