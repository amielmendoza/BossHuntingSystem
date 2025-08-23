using Microsoft.AspNetCore.Http;

namespace BossHuntingSystem.Server.Models
{
    public class VisionExtractRequest
    {
        public IFormFile File { get; set; } = default!;
        public string Mode { get; set; } = "loot"; // "loot" | "attendee"
    }
}


