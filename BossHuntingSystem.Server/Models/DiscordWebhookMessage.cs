namespace BossHuntingSystem.Server.Models
{
    public class DiscordWebhookMessage
    {
        public string Content { get; set; } = string.Empty;
        public DiscordEmbed[]? Embeds { get; set; }
    }

    public class DiscordEmbed
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Color { get; set; }
        public DiscordEmbedField[]? Fields { get; set; }
        public DiscordEmbedFooter? Footer { get; set; }
        public string? Timestamp { get; set; }
    }

    public class DiscordEmbedField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Inline { get; set; } = false;
    }

    public class DiscordEmbedFooter
    {
        public string Text { get; set; } = string.Empty;
    }
}
