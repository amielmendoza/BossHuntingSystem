namespace BossHuntingSystem.Server.Models
{
    public class IpRestrictionsConfig
    {
        public bool Enabled { get; set; } = false;
        public List<string> AllowedIps { get; set; } = new List<string>();
        public List<string> RestrictedEndpoints { get; set; } = new List<string>();
    }
}
