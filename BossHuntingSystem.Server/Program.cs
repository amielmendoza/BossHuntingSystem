using BossHuntingSystem.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
            // Production: Allow your Azure Web App domain
            policy.WithOrigins(
                    "https://bosshuntingsystem.azurewebsites.net",
                    "https://bosshuntingsystem-bbeeekgbb0atcngn.scm.southeastasia-01.azurewebsites.net")
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

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

app.UseCors("AllowedOrigins");

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
