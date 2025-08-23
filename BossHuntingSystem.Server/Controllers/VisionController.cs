using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BossHuntingSystem.Server.Models;
using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisionController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VisionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Response model moved to Models/ to avoid nested class issues in Swagger

        [HttpPost("extract")]
        [RequestSizeLimit(20_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Extract([FromForm] VisionExtractRequest request)
        {
            var file = request.File;
            var mode = string.IsNullOrWhiteSpace(request.Mode) ? "loot" : request.Mode;
            if (file == null || file.Length == 0) return BadRequest("File is required");

            // Azure Computer Vision configuration
            var visionEndpoint = _configuration["AZURE_VISION_ENDPOINT"] ?? Environment.GetEnvironmentVariable("AZURE_VISION_ENDPOINT");
            var visionKey = _configuration["AZURE_VISION_API_KEY"] ?? Environment.GetEnvironmentVariable("AZURE_VISION_API_KEY");
            if (string.IsNullOrWhiteSpace(visionEndpoint) || string.IsNullOrWhiteSpace(visionKey))
            {
                return StatusCode(501, new { error = "Azure Vision not configured. Set AZURE_VISION_ENDPOINT and AZURE_VISION_API_KEY." });
            }

            byte[] bytes;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            // Analyze image using Azure Computer Vision OCR
            var client = new ImageAnalysisClient(new Uri(visionEndpoint!), new AzureKeyCredential(visionKey!));
            var analysis = await client.AnalyzeAsync(BinaryData.FromBytes(bytes), VisualFeatures.Read);

            var sb = new StringBuilder();
            if (analysis?.Value?.Read != null)
            {
                foreach (var block in analysis.Value.Read.Blocks)
                {
                    foreach (var line in block.Lines)
                    {
                        sb.AppendLine(line.Text);
                    }
                }
            }

            var text = sb.ToString();
            var loots = ParseLootFromText(text);
            var attendees = ParseAttendeesFromText(text);

            // If user picked a specific mode, prefer that array and leave the other as-is (parsed anyway)
            var response = new ExtractResponse
            {
                Loots = loots.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Attendees = attendees.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };
            return Ok(response);
        }

        private static List<string> ParseLootFromText(string text)
        {
            var items = new List<string>();
            var lines = text.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var lootRegex = new System.Text.RegularExpressions.Regex("(acquired|obtained|acquire|loot(ed)?|received)\\s+(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var line in lines)
            {
                var m = lootRegex.Match(line);
                if (m.Success)
                {
                    var item = m.Groups[3].Value;
                    item = System.Text.RegularExpressions.Regex.Replace(item, "\\s+from\\s+.+$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    item = System.Text.RegularExpressions.Regex.Replace(item, "\\[[0-9:]+\\]\\s*", string.Empty);
                    item = System.Text.RegularExpressions.Regex.Replace(item, "\\s{2,}", " ");
                    if (!string.IsNullOrWhiteSpace(item)) items.Add(item);
                }
            }
            return items;
        }

        private static List<string> ParseAttendeesFromText(string text)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = text.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var raw in lines)
            {
                var line = System.Text.RegularExpressions.Regex.Replace(raw, "^\\s*\\[[0-9:]+\\]\\s*", string.Empty);
                line = System.Text.RegularExpressions.Regex.Replace(line, "\\s{2,}", " ").Trim();
                var m = System.Text.RegularExpressions.Regex.Match(line, "^(\\S+)\\s+(acquired|obtained|received|looted|gets|got)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var name = System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, "[^A-Za-z0-9_\\-]", string.Empty);
                    if (!string.IsNullOrWhiteSpace(name)) { names.Add(name); continue; }
                }
                if (System.Text.RegularExpressions.Regex.IsMatch(line, "^[^\\s]+$"))
                {
                    var token = System.Text.RegularExpressions.Regex.Replace(line, "[^A-Za-z0-9_\\-]", string.Empty);
                    if (!string.IsNullOrWhiteSpace(token) && !System.Text.RegularExpressions.Regex.IsMatch(token, "^\\d+$") && token.Length >= 2 && token.Length <= 20)
                    {
                        names.Add(token);
                    }
                }
            }
            return names.ToList();
        }
    }
}


