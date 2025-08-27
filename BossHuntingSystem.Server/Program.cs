using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Models;
using BossHuntingSystem.Server.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Entity Framework
builder.Services.AddDbContext<BossHuntingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(
                    "https://localhost:53931",
                    "https://127.0.0.1:53931")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Production: Allow your Azure Web App domain (temporarily more permissive for debugging)
            policy.WithOrigins(
                    "https://bosshuntingsystem.azurewebsites.net",
                    "https://bosshuntingsystem-bbeeekgbb0atcngn.southeastasia-01.azurewebsites.net",
                    "https://bosshuntingsystem-bbeeekgbb0atcngn.scm.southeastasia-01.azurewebsites.net")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
            
            // Temporary: Add wildcard for Azure subdomains to help debug
            policy.SetIsOriginAllowed(origin => 
                origin.Contains("azurewebsites.net") || 
                origin.Contains("bosshuntingsystem"))
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});
builder.Services.AddHttpClient();

// Discord notification services
builder.Services.AddHttpClient<IDiscordNotificationService, DiscordNotificationService>();
builder.Services.AddSingleton<IBossNotificationTracker, BossNotificationTracker>();
builder.Services.AddHostedService<BossNotificationBackgroundService>();

// IP Restrictions configuration
try
{
    builder.Services.Configure<IpRestrictionsConfig>(
        builder.Configuration.GetSection("IpRestrictions"));
    Console.WriteLine("[Program] IP Restrictions configuration loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[Program] Error loading IP Restrictions configuration: {ex.Message}");
    // Continue without IP restrictions if configuration fails
}

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

// Add IP restriction middleware (with error handling)
try
{
    app.UseMiddleware<IpRestrictionMiddleware>();
    Console.WriteLine("[Program] IP restriction middleware registered successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[Program] Error registering IP restriction middleware: {ex.Message}");
    // Continue without IP restrictions if middleware fails
}

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
