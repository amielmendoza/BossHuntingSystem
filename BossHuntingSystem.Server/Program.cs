using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Entity Framework
builder.Services.AddDbContext<BossHuntingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpContextAccessor for TenantContext
builder.Services.AddHttpContextAccessor();

// Register services
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Add detailed logging for authentication events
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT] Authentication failed: {context.Exception.Message}");
            Console.WriteLine($"[JWT] Exception type: {context.Exception.GetType().Name}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"[JWT] Inner exception: {context.Exception.InnerException.Message}");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"[JWT] Token validated successfully for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            Console.WriteLine($"[JWT] Message received - Authorization header: {(authHeader != null ? authHeader.Substring(0, Math.Min(30, authHeader.Length)) + "..." : "NONE")}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"[JWT] Challenge issued - Error: {context.Error}, ErrorDescription: {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(
                    "https://localhost:53931",
                    "https://127.0.0.1:53931",
                    "https://localhost:7294",
                    "https://127.0.0.1:7294",
                    "http://localhost:5077",
                    "http://127.0.0.1:5077")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Production: Allow your Windows Server domain
            policy.WithOrigins(
                    "https://devdrix.com",
                    "http://devdrix.com",
                    "https://localhost",
                    "http://localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});
// Discord notification services
builder.Services.AddHttpClient<IDiscordNotificationService, DiscordNotificationService>();
builder.Services.AddSingleton<IBossNotificationTracker, BossNotificationTracker>();
builder.Services.AddHostedService<BossNotificationBackgroundService>();



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migrate database and populate loot items data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BossHuntingDbContext>();
    context.Database.Migrate();
    
    // Populate LootItemsJson from existing LootsJson data
    var recordsToUpdate = context.BossDefeats
        .Where(r => (string.IsNullOrEmpty(r.LootItemsJson) || r.LootItemsJson == "[]") && 
                    !string.IsNullOrEmpty(r.LootsJson) && r.LootsJson != "[]")
        .ToList();
    
    foreach (var record in recordsToUpdate)
    {
        try
        {
            var loots = record.Loots;
            var lootItems = loots.Select(loot => new BossHuntingSystem.Server.Data.LootItem { Name = loot, Price = null }).ToList();
            record.LootItems = lootItems;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating record {record.Id}: {ex.Message}");
        }
    }
    
    if (recordsToUpdate.Any())
    {
        context.SaveChanges();
        Console.WriteLine($"Updated {recordsToUpdate.Count} records with loot items data");
    }
}

app.UseDefaultFiles();

// Configure static file options with proper MIME types
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".mjs"] = "application/javascript";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production error handling
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();



// Add request logging middleware for debugging
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.FirstOrDefault() ?? "unknown";
    Console.WriteLine($"[Request] {context.Request.Method} {context.Request.Path} from {origin}");
    Console.WriteLine($"[Request] Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}:{string.Join(",", h.Value.ToArray())}"))}");
    
    await next();
    
    Console.WriteLine($"[Response] Status: {context.Response.StatusCode}");
});

app.UseCors("AllowedOrigins");

app.UseRouting();

app.UseAuthentication();

// Add tenant context middleware (must be after authentication)
app.UseTenantContext();

// Add license validation middleware (must be after tenant context)
app.UseLicenseValidation();

app.UseAuthorization();

app.MapControllers();

// SPA fallback routing - this should be LAST
app.MapFallback(async context =>
{
    // Only serve fallback for non-API and non-static file requests
    var path = context.Request.Path.Value?.ToLower() ?? "";
    
    if (path.StartsWith("/api/") || 
        path.EndsWith(".js") || 
        path.EndsWith(".css") || 
        path.EndsWith(".png") || 
        path.EndsWith(".jpg") || 
        path.EndsWith(".ico") ||
        path.EndsWith(".map") ||
        path.EndsWith(".json"))
    {
        context.Response.StatusCode = 404;
        return;
    }
    
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.Run();
