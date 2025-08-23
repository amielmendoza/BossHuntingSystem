using System.Text;
using System.Text.Json;
using BossHuntingSystem.Server.Models;

namespace BossHuntingSystem.Server.Services
{
    public interface IDiscordNotificationService
    {
        Task SendBossNotificationAsync(string bossName, int minutesUntilRespawn);
        Task SendManualNotificationAsync(string message);
    }

    public class DiscordNotificationService : IDiscordNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiscordNotificationService> _logger;

        public DiscordNotificationService(HttpClient httpClient, IConfiguration configuration, ILogger<DiscordNotificationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendBossNotificationAsync(string bossName, int minutesUntilRespawn)
        {
            var webhookUrl = _configuration["DISCORD_WEBHOOK_URL"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Discord webhook URL not configured");
                return;
            }

            try
            {
                var message = CreateBossNotificationMessage(bossName, minutesUntilRespawn);
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Discord notification sent for {BossName} ({Minutes} minutes)", bossName, minutesUntilRespawn);
                }
                else
                {
                    _logger.LogError("Failed to send Discord notification: {StatusCode} - {Content}", 
                        response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Discord notification for {BossName}", bossName);
            }
        }

        private DiscordWebhookMessage CreateBossNotificationMessage(string bossName, int minutesUntilRespawn)
        {
            var color = minutesUntilRespawn switch
            {
                1 => 0xFF0000,  // Red for 1 minute
                5 => 0xFF6600,  // Orange for 5 minutes
                10 => 0xFFCC00, // Yellow for 10 minutes
                20 => 0x0099FF, // Blue for 20 minutes
                30 => 0x00FF00, // Green for 30 minutes
                _ => 0x808080   // Gray default
            };

            var urgencyText = minutesUntilRespawn switch
            {
                1 => "🚨 **URGENT** 🚨",
                5 => "⚠️ **INCOMING** ⚠️",
                10 => "🔔 **SOON** 🔔",
                20 => "📢 **HEADS UP** 📢",
                30 => "ℹ️ **NOTICE** ℹ️",
                _ => "📝 **UPDATE** 📝"
            };

            var timeText = minutesUntilRespawn == 1 ? "1 minute" : $"{minutesUntilRespawn} minutes";

            // Add @everyone for urgent notifications (1 and 5 minutes)
            var mention = (minutesUntilRespawn <= 5) ? "@everyone " : "";

            return new DiscordWebhookMessage
            {
                Content = $"{mention}{urgencyText} **{bossName}** respawning in **{timeText}**! @everyone",
                Embeds = new[]
                {
                    new DiscordEmbed
                    {
                        Title = $"🐉 {bossName}",
                        Description = $"**Respawns in:** {timeText}",
                        Color = color,
                        Fields = new[]
                        {
                            new DiscordEmbedField
                            {
                                Name = "⏰ Time Remaining",
                                Value = timeText,
                                Inline = true
                            },
                            new DiscordEmbedField
                            {
                                Name = "🐉 Boss Name",
                                Value = bossName,
                                Inline = true
                            }
                        },
                        Footer = new DiscordEmbedFooter
                        {
                            Text = "Boss Hunting System"
                        },
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                }
            };
        }

        public async Task SendManualNotificationAsync(string message)
        {
            var webhookUrl = _configuration["DISCORD_WEBHOOK_URL"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Discord webhook URL not configured");
                return;
            }

            try
            {
                var discordMessage = new DiscordWebhookMessage
                {
                    Content = "@everyone " + message,
                    Embeds = new[]
                    {
                        new DiscordEmbed
                        {
                            Title = "📢 Manual Notification",
                            Description = message,
                            Color = 0x00FF00, // Green color
                            Footer = new DiscordEmbedFooter
                            {
                                Text = "Boss Hunting System - Manual"
                            },
                            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }
                    }
                };

                var json = JsonSerializer.Serialize(discordMessage, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Manual Discord notification sent: {Message}", message);
                }
                else
                {
                    _logger.LogError("Failed to send manual Discord notification: {StatusCode} - {Content}", 
                        response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending manual Discord notification: {Message}", message);
            }
        }
    }
}
