using BossHuntingSystem.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace BossHuntingSystem.Server.Models
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime LicenseExpirationDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? CompanyName { get; set; }
        public string? Notes { get; set; }
        public int GracePeriodDays { get; set; }
        public int UserCount { get; set; }
        public int MemberCount { get; set; }
        public int BossCount { get; set; }
        public LicenseStatus? LicenseStatus { get; set; }
    }

    public class CreateClientRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime LicenseExpirationDate { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string ContactEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? CompanyName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int? GracePeriodDays { get; set; } = 7;
    }

    public class UpdateClientRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        public DateTime? LicenseExpirationDate { get; set; }

        public bool? IsActive { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string? ContactEmail { get; set; }

        [MaxLength(100)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? CompanyName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int? GracePeriodDays { get; set; }
    }

    public class RenewLicenseRequest
    {
        [Required]
        public DateTime NewExpirationDate { get; set; }
    }
}
