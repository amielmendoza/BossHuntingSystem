using BossHuntingSystem.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace BossHuntingSystem.Server.Services
{
    public interface ILicenseService
    {
        Task<LicenseStatus> ValidateLicenseAsync(int clientId);
        Task<bool> IsLicenseActiveAsync(int clientId);
        Task<bool> IsInGracePeriodAsync(int clientId);
        Task<int> GetRemainingDaysAsync(int clientId);
        string GenerateLicenseKey();
    }

    public class LicenseService : ILicenseService
    {
        private readonly BossHuntingDbContext _context;

        public LicenseService(BossHuntingDbContext context)
        {
            _context = context;
        }

        public async Task<LicenseStatus> ValidateLicenseAsync(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);

            if (client == null)
            {
                return new LicenseStatus
                {
                    IsValid = false,
                    IsExpired = true,
                    IsInGracePeriod = false,
                    Message = "Client not found"
                };
            }

            if (!client.IsActive)
            {
                return new LicenseStatus
                {
                    IsValid = false,
                    IsExpired = true,
                    IsInGracePeriod = false,
                    Message = "Client account is inactive"
                };
            }

            var now = DateTime.UtcNow;
            var expirationDate = client.LicenseExpirationDate;
            var gracePeriodEnd = expirationDate.AddDays(client.GracePeriodDays);

            if (now <= expirationDate)
            {
                var daysRemaining = (expirationDate - now).Days;
                return new LicenseStatus
                {
                    IsValid = true,
                    IsExpired = false,
                    IsInGracePeriod = false,
                    DaysRemaining = daysRemaining,
                    ExpirationDate = expirationDate,
                    Message = daysRemaining <= 7
                        ? $"License expires in {daysRemaining} days"
                        : "License is active"
                };
            }
            else if (now <= gracePeriodEnd)
            {
                var graceDaysRemaining = (gracePeriodEnd - now).Days;
                return new LicenseStatus
                {
                    IsValid = true,
                    IsExpired = false,
                    IsInGracePeriod = true,
                    DaysRemaining = graceDaysRemaining,
                    ExpirationDate = expirationDate,
                    GracePeriodEnd = gracePeriodEnd,
                    Message = $"License expired. Grace period: {graceDaysRemaining} days remaining (Read-only mode)"
                };
            }
            else
            {
                return new LicenseStatus
                {
                    IsValid = false,
                    IsExpired = true,
                    IsInGracePeriod = false,
                    ExpirationDate = expirationDate,
                    Message = "License has expired. Please contact support to renew."
                };
            }
        }

        public async Task<bool> IsLicenseActiveAsync(int clientId)
        {
            var status = await ValidateLicenseAsync(clientId);
            return status.IsValid && !status.IsInGracePeriod;
        }

        public async Task<bool> IsInGracePeriodAsync(int clientId)
        {
            var status = await ValidateLicenseAsync(clientId);
            return status.IsInGracePeriod;
        }

        public async Task<int> GetRemainingDaysAsync(int clientId)
        {
            var status = await ValidateLicenseAsync(clientId);
            return status.DaysRemaining;
        }

        public string GenerateLicenseKey()
        {
            // Generate a unique license key (format: XXXX-XXXX-XXXX-XXXX)
            var guid = Guid.NewGuid().ToString("N").ToUpper();
            return $"{guid.Substring(0, 4)}-{guid.Substring(4, 4)}-{guid.Substring(8, 4)}-{guid.Substring(12, 4)}";
        }
    }

    public class LicenseStatus
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public bool IsInGracePeriod { get; set; }
        public int DaysRemaining { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime? GracePeriodEnd { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
