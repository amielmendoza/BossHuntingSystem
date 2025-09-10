using System.Text;
using System.Text.Json;
using BossHuntingSystem.Server.Models;
using BossHuntingSystem.Server.Controllers;

namespace BossHuntingSystem.Server.Services
{
    public interface IDiscordNotificationService
    {
        Task SendBossNotificationAsync(string bossName, int minutesUntilRespawn, string? owner = null);
        Task SendManualNotificationAsync(string message);
        Task SendDailyPointsSummaryAsync(List<MemberPointsDto> memberPoints);
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

        public async Task SendBossNotificationAsync(string bossName, int minutesUntilRespawn, string? owner = null)
        {
            var webhookUrl = _configuration["DISCORD_WEBHOOK_URL"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Discord webhook URL not configured");
                return;
            }

            try
            {
                var message = CreateBossNotificationMessage(bossName, minutesUntilRespawn, owner);
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Discord notification sent for {BossName} ({Minutes} minutes) owned by {Owner}", 
                        bossName, minutesUntilRespawn, owner ?? "Unknown");
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

        private DiscordWebhookMessage CreateBossNotificationMessage(string bossName, int minutesUntilRespawn, string? owner = null)
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

            // Build owner text
            var ownerText = !string.IsNullOrWhiteSpace(owner) ? $" (Owned by **{owner}**)" : "";
            var ownerDescription = !string.IsNullOrWhiteSpace(owner) ? $"\n**Owned by:** {owner}" : "";

            // Create fields list
            var fields = new List<DiscordEmbedField>
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
            };

            // Add owner field if owner exists
            if (!string.IsNullOrWhiteSpace(owner))
            {
                fields.Add(new DiscordEmbedField
                {
                    Name = "👑 Owned By",
                    Value = owner,
                    Inline = true
                });
            }

            return new DiscordWebhookMessage
            {
                Content = $"{mention}{urgencyText} **{bossName}**{ownerText} respawning in **{timeText}**! @everyone",
                Embeds = new[]
                {
                    new DiscordEmbed
                    {
                        Title = $"🐉 {bossName}",
                        Description = $"**Respawns in:** {timeText}{ownerDescription}",
                        Color = color,
                        Fields = fields.ToArray(),
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

        public async Task SendDailyPointsSummaryAsync(List<MemberPointsDto> memberPoints)
        {
            var webhookUrl = _configuration["DISCORD_POINTS_WEBHOOK_URL"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("Discord points webhook URL not configured for points summary");
                return;
            }

            try
            {
                var message = CreatePointsSummaryMessage(memberPoints);
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Points summary sent to Discord for {MemberCount} members", memberPoints.Count);
                }
                else
                {
                    _logger.LogError("Failed to send points summary: {StatusCode} - {Content}", 
                        response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending points summary to Discord");
            }
        }

        private DiscordWebhookMessage CreatePointsSummaryMessage(List<MemberPointsDto> memberPoints)
        {
            var totalMembers = memberPoints.Count;
            var totalPoints = memberPoints.Sum(m => m.Points);
            var totalBossesAttended = memberPoints.Sum(m => m.BossesAttended);

            // Sort by points descending for leaderboard
            var sortedMembers = memberPoints.OrderByDescending(m => m.Points).ToList();
            
            // Create fields for top performers
            var fields = new List<DiscordEmbedField>();
            
            // Add summary statistics
            fields.Add(new DiscordEmbedField
            {
                Name = "📊 Summary Statistics",
                Value = $"**Total Members:** {totalMembers}\n**Total Points Earned:** {totalPoints:F1}\n**Total Boss Fights:** {totalBossesAttended}",
                Inline = false
            });

            // Add top 10 performers
            var topPerformersText = "";
            for (int i = 0; i < Math.Min(10, sortedMembers.Count); i++)
            {
                var member = sortedMembers[i];
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈", 
                    2 => "🥉",
                    _ => $"**{i + 1}.**"
                };
                topPerformersText += $"{medal} {member.MemberName} - **{member.Points:F1}** pts ({member.BossesAttended} fights)\n";
            }

            if (!string.IsNullOrEmpty(topPerformersText))
            {
                fields.Add(new DiscordEmbedField
                {
                    Name = "🏆 Top Performers",
                    Value = topPerformersText.Trim(),
                    Inline = false
                });
            }

            // Add all members if <= 15, otherwise show message
            if (totalMembers <= 15 && totalMembers > 10)
            {
                var remainingText = "";
                for (int i = 10; i < sortedMembers.Count; i++)
                {
                    var member = sortedMembers[i];
                    remainingText += $"**{i + 1}.** {member.MemberName} - {member.Points:F1} pts ({member.BossesAttended})\n";
                }
                
                if (!string.IsNullOrEmpty(remainingText))
                {
                    fields.Add(new DiscordEmbedField
                    {
                        Name = "📋 Other Members",
                        Value = remainingText.Trim(),
                        Inline = false
                    });
                }
            }
            else if (totalMembers > 15)
            {
                fields.Add(new DiscordEmbedField
                {
                    Name = "ℹ️ Additional Members",
                    Value = $"+ {totalMembers - 10} more members not shown",
                    Inline = false
                });
            }

            var phtNow = DateTime.UtcNow.AddHours(8);
            var timeOfDay = phtNow.Hour switch
            {
                >= 0 and < 6 => "🌙 Midnight Update",
                >= 6 and < 12 => "🌅 Morning Update", 
                >= 12 and < 18 => "☀️ Afternoon Update",
                _ => "🌆 Evening Update"
            };

            return new DiscordWebhookMessage
            {
                Content = $"📈 **Points Summary** - {timeOfDay} - Here's how everyone is performing!",
                Embeds = new[]
                {
                    new DiscordEmbed
                    {
                        Title = "🎮 Guild Activity Report",
                        Description = $"Current standings of member performance and boss fight participation\n*Generated at {phtNow:MMM dd, yyyy HH:mm} PHT*",
                        Color = 0x00D4AA, // Teal color
                        Fields = fields.ToArray(),
                        Footer = new DiscordEmbedFooter
                        {
                            Text = "Boss Hunting System - 6-Hour Updates"
                        },
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                }
            };
        }
    }
}
