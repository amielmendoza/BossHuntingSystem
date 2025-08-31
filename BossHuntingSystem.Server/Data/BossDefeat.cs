using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BossHuntingSystem.Server.Data
{
    public class BossDefeat
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int BossId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string BossName { get; set; } = string.Empty;
        
        // Nullable DateTime - null for history entries, actual time for defeats
        public DateTime? DefeatedAtUtc { get; set; }
        
        // Store loots and attendees as JSON strings
        [Column(TypeName = "nvarchar(max)")]
        public string LootsJson { get; set; } = "[]";
        
        [Column(TypeName = "nvarchar(max)")]
        public string AttendeesJson { get; set; } = "[]";
        
        // New property for loot items with prices
        [Column(TypeName = "nvarchar(max)")]
        public string LootItemsJson { get; set; } = "[]";
        
        [MaxLength(100)]
        public string? Killer { get; set; }
        
        // Navigation property
        [ForeignKey("BossId")]
        public virtual Boss? Boss { get; set; }
        
        // Not mapped properties for easy access (will be serialized to/from JSON)
        [NotMapped]
        public List<string> Loots
        {
            get
            {
                if (string.IsNullOrEmpty(LootsJson) || LootsJson == "[]")
                {
                    return new List<string>();
                }
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(LootsJson) ?? new List<string>();
                }
                catch
                {
                    // If JSON is corrupted, return empty list
                    return new List<string>();
                }
            }
            set => LootsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
        
        [NotMapped]
        public List<string> Attendees
        {
            get
            {
                if (string.IsNullOrEmpty(AttendeesJson) || AttendeesJson == "[]")
                {
                    return new List<string>();
                }
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(AttendeesJson) ?? new List<string>();
                }
                catch
                {
                    // If JSON is corrupted, return empty list
                    return new List<string>();
                }
            }
            set => AttendeesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
        
        [NotMapped]
        public List<LootItem> LootItems
        {
            get
            {
                if (string.IsNullOrEmpty(LootItemsJson) || LootItemsJson == "[]")
                {
                    return new List<LootItem>();
                }
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<LootItem>>(LootItemsJson) ?? new List<LootItem>();
                }
                catch
                {
                    // If JSON is corrupted, return empty list
                    return new List<LootItem>();
                }
            }
            set => LootItemsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
    
    public class LootItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
    }
}
