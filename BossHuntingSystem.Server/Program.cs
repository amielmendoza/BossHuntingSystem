using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Entity Framework
builder.Services.AddDbContext<BossHuntingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Authentication:JwtSettings"));

var jwtSettings = builder.Configuration.GetSection("Authentication:JwtSettings").Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings?.SecretKey ?? "default-secret-key");

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings?.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

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
                    "https://your-server-domain.com",
                    "http://your-server-domain.com",
                    "https://localhost",
                    "http://localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});
builder.Services.AddHttpClient();

// Discord notification services
builder.Services.AddHttpClient<IDiscordNotificationService, DiscordNotificationService>();
builder.Services.AddSingleton<IBossNotificationTracker, BossNotificationTracker>();
builder.Services.AddHostedService<BossNotificationBackgroundService>();

// Authentication services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    // Admin policy - full access to everything
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));
    
    // User policy - basic authenticated access
    options.AddPolicy("User", policy =>
        policy.RequireRole("User", "Admin"));
    
    // Read-only policy for viewing data
    options.AddPolicy("ReadOnly", policy =>
        policy.RequireRole("User", "Admin"));
    
    // Write policy for modifying data (Admin only)
    options.AddPolicy("Write", policy =>
        policy.RequireRole("Admin"));
    
    // Boss management policy - for boss-related operations
    options.AddPolicy("BossManagement", policy =>
        policy.RequireRole("Admin"));
    
    // Member management policy - for member-related operations
    options.AddPolicy("MemberManagement", policy =>
        policy.RequireRole("Admin"));
    
    // Notification policy - for sending notifications
    options.AddPolicy("Notifications", policy =>
        policy.RequireRole("Admin"));
    
    // System administration policy - for system-level operations
    options.AddPolicy("SystemAdmin", policy =>
        policy.RequireRole("Admin"));
    
    // Data export policy - for exporting sensitive data
    options.AddPolicy("DataExport", policy =>
        policy.RequireRole("Admin"));
});

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

// Add Authentication and Authorization middleware
app.UseAuthentication();
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
