using BossHuntingSystem.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BossHuntingSystem.Server.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(int clientId, string username, string action, string entityType, int? entityId = null, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? additionalInfo = null);
        Task<List<AuditLog>> GetClientAuditLogsAsync(int clientId, int pageNumber = 1, int pageSize = 50);
        Task<List<AuditLog>> GetEntityAuditLogsAsync(int clientId, string entityType, int entityId);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly BossHuntingDbContext _context;

        public AuditLogService(BossHuntingDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(
            int clientId,
            string username,
            string action,
            string entityType,
            int? entityId = null,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            string? additionalInfo = null)
        {
            var auditLog = new AuditLog
            {
                ClientId = clientId,
                Username = username,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                AdditionalInfo = additionalInfo,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetClientAuditLogsAsync(int clientId, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Where(log => log.ClientId == clientId)
                .OrderByDescending(log => log.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetEntityAuditLogsAsync(int clientId, string entityType, int entityId)
        {
            return await _context.AuditLogs
                .Where(log => log.ClientId == clientId && log.EntityType == entityType && log.EntityId == entityId)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
    }
}
