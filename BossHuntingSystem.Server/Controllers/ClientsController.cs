using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Models;
using System.Text.Json;
using ClosedXML.Excel;
using System.IO;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;
        private readonly ILicenseService _licenseService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(
            BossHuntingDbContext context,
            ILicenseService licenseService,
            IAuditLogService auditLogService,
            ILogger<ClientsController> logger)
        {
            _context = context;
            _licenseService = licenseService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        private int? GetCurrentClientId()
        {
            return HttpContext.Items["ClientId"] as int?;
        }

        private string GetCurrentUsername()
        {
            return HttpContext.Items["Username"] as string ?? "Unknown";
        }

        private bool IsSuperAdmin()
        {
            return (HttpContext.Items["UserRole"] as string) == "SuperAdmin";
        }

        // GET: api/clients (SuperAdmin only - view all clients)
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetAllClients()
        {
            var clients = await _context.Clients
                .Include(c => c.Users)
                .Include(c => c.Members)
                .Include(c => c.Bosses)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            var clientDtos = new List<ClientDto>();

            foreach (var client in clients)
            {
                var licenseStatus = await _licenseService.ValidateLicenseAsync(client.Id);

                clientDtos.Add(new ClientDto
                {
                    Id = client.Id,
                    Name = client.Name,
                    LicenseKey = client.LicenseKey,
                    LicenseExpirationDate = client.LicenseExpirationDate,
                    IsActive = client.IsActive,
                    CreatedDate = client.CreatedDate,
                    ContactEmail = client.ContactEmail,
                    ContactPhone = client.ContactPhone,
                    CompanyName = client.CompanyName,
                    Notes = client.Notes,
                    GracePeriodDays = client.GracePeriodDays,
                    UserCount = client.Users.Count,
                    MemberCount = client.Members.Count,
                    BossCount = client.Bosses.Count,
                    LicenseStatus = licenseStatus
                });
            }

            return Ok(clientDtos);
        }

        // GET: api/clients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientDto>> GetClient(int id)
        {
            // SuperAdmin can view any client, others can only view their own
            if (!IsSuperAdmin() && GetCurrentClientId() != id)
            {
                return Forbid();
            }

            var client = await _context.Clients
                .Include(c => c.Users)
                .Include(c => c.Members)
                .Include(c => c.Bosses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            var licenseStatus = await _licenseService.ValidateLicenseAsync(client.Id);

            var clientDto = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                LicenseKey = client.LicenseKey,
                LicenseExpirationDate = client.LicenseExpirationDate,
                IsActive = client.IsActive,
                CreatedDate = client.CreatedDate,
                ContactEmail = client.ContactEmail,
                ContactPhone = client.ContactPhone,
                CompanyName = client.CompanyName,
                Notes = client.Notes,
                GracePeriodDays = client.GracePeriodDays,
                UserCount = client.Users.Count,
                MemberCount = client.Members.Count,
                BossCount = client.Bosses.Count,
                LicenseStatus = licenseStatus
            };

            return Ok(clientDto);
        }

        // POST: api/clients (SuperAdmin only - create new client)
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<ClientDto>> CreateClient(CreateClientRequest request)
        {
            var licenseKey = _licenseService.GenerateLicenseKey();

            var client = new Client
            {
                Name = request.Name,
                LicenseKey = licenseKey,
                LicenseExpirationDate = request.LicenseExpirationDate,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                CompanyName = request.CompanyName,
                Notes = request.Notes,
                GracePeriodDays = request.GracePeriodDays ?? 7
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                client.Id,
                GetCurrentUsername(),
                "CREATE",
                "Client",
                client.Id,
                null,
                client,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            var licenseStatus = await _licenseService.ValidateLicenseAsync(client.Id);

            var clientDto = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                LicenseKey = client.LicenseKey,
                LicenseExpirationDate = client.LicenseExpirationDate,
                IsActive = client.IsActive,
                CreatedDate = client.CreatedDate,
                ContactEmail = client.ContactEmail,
                ContactPhone = client.ContactPhone,
                CompanyName = client.CompanyName,
                Notes = client.Notes,
                GracePeriodDays = client.GracePeriodDays,
                LicenseStatus = licenseStatus
            };

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, clientDto);
        }

        // PUT: api/clients/{id} (SuperAdmin only - update client)
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateClient(int id, UpdateClientRequest request)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            var oldValues = new
            {
                client.Name,
                client.LicenseExpirationDate,
                client.IsActive,
                client.ContactEmail,
                client.ContactPhone,
                client.CompanyName,
                client.Notes,
                client.GracePeriodDays
            };

            client.Name = request.Name ?? client.Name;
            client.LicenseExpirationDate = request.LicenseExpirationDate ?? client.LicenseExpirationDate;
            client.IsActive = request.IsActive ?? client.IsActive;
            client.ContactEmail = request.ContactEmail ?? client.ContactEmail;
            client.ContactPhone = request.ContactPhone ?? client.ContactPhone;
            client.CompanyName = request.CompanyName ?? client.CompanyName;
            client.Notes = request.Notes ?? client.Notes;
            client.GracePeriodDays = request.GracePeriodDays ?? client.GracePeriodDays;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                client.Id,
                GetCurrentUsername(),
                "UPDATE",
                "Client",
                client.Id,
                oldValues,
                new
                {
                    client.Name,
                    client.LicenseExpirationDate,
                    client.IsActive,
                    client.ContactEmail,
                    client.ContactPhone,
                    client.CompanyName,
                    client.Notes,
                    client.GracePeriodDays
                },
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return NoContent();
        }

        // POST: api/clients/{id}/renew-license (SuperAdmin only)
        [HttpPost("{id}/renew-license")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RenewLicense(int id, RenewLicenseRequest request)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            var oldExpiration = client.LicenseExpirationDate;
            client.LicenseExpirationDate = request.NewExpirationDate;
            client.IsActive = true;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                client.Id,
                GetCurrentUsername(),
                "RENEW_LICENSE",
                "Client",
                client.Id,
                new { LicenseExpirationDate = oldExpiration },
                new { LicenseExpirationDate = request.NewExpirationDate },
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                $"License renewed from {oldExpiration:yyyy-MM-dd} to {request.NewExpirationDate:yyyy-MM-dd}"
            );

            return Ok(new { message = "License renewed successfully", newExpirationDate = request.NewExpirationDate });
        }

        // POST: api/clients/{id}/deactivate (SuperAdmin only)
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeactivateClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            client.IsActive = false;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                client.Id,
                GetCurrentUsername(),
                "DEACTIVATE",
                "Client",
                client.Id,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Client account deactivated"
            );

            return Ok(new { message = "Client deactivated successfully" });
        }

        // POST: api/clients/{id}/activate (SuperAdmin only)
        [HttpPost("{id}/activate")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ActivateClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            client.IsActive = true;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                client.Id,
                GetCurrentUsername(),
                "ACTIVATE",
                "Client",
                client.Id,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Client account activated"
            );

            return Ok(new { message = "Client activated successfully" });
        }

        // GET: api/clients/{id}/license-status
        [HttpGet("{id}/license-status")]
        public async Task<ActionResult<LicenseStatus>> GetLicenseStatus(int id)
        {
            // Users can only check their own client's license status
            if (!IsSuperAdmin() && GetCurrentClientId() != id)
            {
                return Forbid();
            }

            var licenseStatus = await _licenseService.ValidateLicenseAsync(id);
            return Ok(licenseStatus);
        }

        // GET: api/clients/my-license (Current user's license status)
        [HttpGet("my-license")]
        public async Task<ActionResult<LicenseStatus>> GetMyLicenseStatus()
        {
            var clientId = GetCurrentClientId();

            if (clientId == null)
            {
                return BadRequest("Client context not found");
            }

            var licenseStatus = await _licenseService.ValidateLicenseAsync(clientId.Value);
            return Ok(licenseStatus);
        }

        // GET: api/clients/audit-logs (Get audit logs with filtering and pagination)
        [HttpGet("audit-logs")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> GetAuditLogs(
            [FromQuery] int? clientId = null,
            [FromQuery] string? username = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var currentClientId = GetCurrentClientId();
            var isSuperAdmin = IsSuperAdmin();

            // Build query
            var query = _context.AuditLogs.AsQueryable();

            // SuperAdmin can see all clients, others only their own
            if (!isSuperAdmin)
            {
                query = query.Where(a => a.ClientId == currentClientId);
            }
            else if (clientId.HasValue)
            {
                query = query.Where(a => a.ClientId == clientId.Value);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(username))
            {
                query = query.Where(a => a.Username.Contains(username));
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and get results
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.Client)
                .Select(a => new
                {
                    a.Id,
                    a.ClientId,
                    ClientName = a.Client != null ? a.Client.Name : null,
                    a.Username,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.OldValues,
                    a.NewValues,
                    a.IpAddress,
                    a.Timestamp
                })
                .ToListAsync();

            return Ok(new { logs, totalCount });
        }

        // GET: api/clients/{id}/audit-logs (Get audit logs for a specific client)
        [HttpGet("{id}/audit-logs")]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetClientAuditLogs(int id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            // Users can only view their own client's audit logs
            if (!IsSuperAdmin() && GetCurrentClientId() != id)
            {
                return Forbid();
            }

            var auditLogs = await _auditLogService.GetClientAuditLogsAsync(id, pageNumber, pageSize);
            return Ok(auditLogs);
        }

        // GET: api/clients/audit-logs/export (Export audit logs to Excel)
        [HttpGet("audit-logs/export")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] int? clientId = null,
            [FromQuery] string? username = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var currentClientId = GetCurrentClientId();
            var isSuperAdmin = IsSuperAdmin();

            // Build query
            var query = _context.AuditLogs.AsQueryable();

            // SuperAdmin can see all clients, others only their own
            if (!isSuperAdmin)
            {
                query = query.Where(a => a.ClientId == currentClientId);
            }
            else if (clientId.HasValue)
            {
                query = query.Where(a => a.ClientId == clientId.Value);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(username))
            {
                query = query.Where(a => a.Username.Contains(username));
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Include(a => a.Client)
                .ToListAsync();

            // Create Excel workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Audit Logs");

            // Headers
            worksheet.Cell(1, 1).Value = "Timestamp";
            worksheet.Cell(1, 2).Value = "Client ID";
            worksheet.Cell(1, 3).Value = "Client Name";
            worksheet.Cell(1, 4).Value = "Username";
            worksheet.Cell(1, 5).Value = "Action";
            worksheet.Cell(1, 6).Value = "Entity Type";
            worksheet.Cell(1, 7).Value = "Entity ID";
            worksheet.Cell(1, 8).Value = "IP Address";
            worksheet.Cell(1, 9).Value = "Old Values";
            worksheet.Cell(1, 10).Value = "New Values";

            // Data
            int row = 2;
            foreach (var log in logs)
            {
                worksheet.Cell(row, 1).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 2).Value = log.ClientId;
                worksheet.Cell(row, 3).Value = log.Client?.Name ?? "";
                worksheet.Cell(row, 4).Value = log.Username;
                worksheet.Cell(row, 5).Value = log.Action;
                worksheet.Cell(row, 6).Value = log.EntityType;
                worksheet.Cell(row, 7).Value = log.EntityId?.ToString() ?? "";
                worksheet.Cell(row, 8).Value = log.IpAddress;
                worksheet.Cell(row, 9).Value = log.OldValues ?? "";
                worksheet.Cell(row, 10).Value = log.NewValues ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save to stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AuditLogs_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }

        // GET: api/clients/{id}/export (Export client data)
        [HttpGet("{id}/export")]
        public async Task<IActionResult> ExportClientData(int id)
        {
            // Users can only export their own client's data
            if (!IsSuperAdmin() && GetCurrentClientId() != id)
            {
                return Forbid();
            }

            var client = await _context.Clients
                .Include(c => c.Members)
                .Include(c => c.Bosses)
                .Include(c => c.BossDefeats)
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            // Create Excel workbook
            using var workbook = new XLWorkbook();

            // Client Info sheet
            var clientSheet = workbook.Worksheets.Add("Client Info");
            clientSheet.Cell(1, 1).Value = "Client Name";
            clientSheet.Cell(1, 2).Value = client.Name;
            clientSheet.Cell(2, 1).Value = "License Key";
            clientSheet.Cell(2, 2).Value = client.LicenseKey;
            clientSheet.Cell(3, 1).Value = "License Expiration";
            clientSheet.Cell(3, 2).Value = client.LicenseExpirationDate.ToString("yyyy-MM-dd");
            clientSheet.Cell(4, 1).Value = "Contact Email";
            clientSheet.Cell(4, 2).Value = client.ContactEmail;
            clientSheet.Cell(5, 1).Value = "Company Name";
            clientSheet.Cell(5, 2).Value = client.CompanyName ?? "";

            // Members sheet
            var membersSheet = workbook.Worksheets.Add("Members");
            membersSheet.Cell(1, 1).Value = "ID";
            membersSheet.Cell(1, 2).Value = "Name";
            membersSheet.Cell(1, 3).Value = "Combat Power";
            membersSheet.Cell(1, 4).Value = "GCash Number";
            membersSheet.Cell(1, 5).Value = "GCash Name";

            int row = 2;
            foreach (var member in client.Members)
            {
                membersSheet.Cell(row, 1).Value = member.Id;
                membersSheet.Cell(row, 2).Value = member.Name;
                membersSheet.Cell(row, 3).Value = member.CombatPower;
                membersSheet.Cell(row, 4).Value = member.GcashNumber ?? "";
                membersSheet.Cell(row, 5).Value = member.GcashName ?? "";
                row++;
            }

            // Bosses sheet
            var bossesSheet = workbook.Worksheets.Add("Bosses");
            bossesSheet.Cell(1, 1).Value = "ID";
            bossesSheet.Cell(1, 2).Value = "Name";
            bossesSheet.Cell(1, 3).Value = "Respawn Hours";
            bossesSheet.Cell(1, 4).Value = "Last Killed At";
            bossesSheet.Cell(1, 5).Value = "Owner";

            row = 2;
            foreach (var boss in client.Bosses)
            {
                bossesSheet.Cell(row, 1).Value = boss.Id;
                bossesSheet.Cell(row, 2).Value = boss.Name;
                bossesSheet.Cell(row, 3).Value = boss.RespawnHours;
                bossesSheet.Cell(row, 4).Value = boss.LastKilledAt.ToString("yyyy-MM-dd HH:mm:ss");
                bossesSheet.Cell(row, 5).Value = boss.Owner ?? "";
                row++;
            }

            // Boss Defeats sheet
            var defeatsSheet = workbook.Worksheets.Add("Boss Defeats");
            defeatsSheet.Cell(1, 1).Value = "ID";
            defeatsSheet.Cell(1, 2).Value = "Boss Name";
            defeatsSheet.Cell(1, 3).Value = "Defeated At";
            defeatsSheet.Cell(1, 4).Value = "Owner";
            defeatsSheet.Cell(1, 5).Value = "Attendees";
            defeatsSheet.Cell(1, 6).Value = "Loots";

            row = 2;
            foreach (var defeat in client.BossDefeats)
            {
                defeatsSheet.Cell(row, 1).Value = defeat.Id;
                defeatsSheet.Cell(row, 2).Value = defeat.BossName;
                defeatsSheet.Cell(row, 3).Value = defeat.DefeatedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                defeatsSheet.Cell(row, 4).Value = defeat.Owner ?? "";
                defeatsSheet.Cell(row, 5).Value = defeat.AttendeesJson;
                defeatsSheet.Cell(row, 6).Value = defeat.LootsJson;
                row++;
            }

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            // Log the export
            await _auditLogService.LogActionAsync(
                id,
                GetCurrentUsername(),
                "EXPORT",
                "Client",
                id,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Client data exported to Excel"
            );

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Client_{client.Name}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }

        // DELETE: api/clients/{id} (SuperAdmin only - soft delete by deactivating)
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            // Soft delete - just deactivate
            client.IsActive = false;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                client.Id,
                GetCurrentUsername(),
                "DELETE",
                "Client",
                client.Id,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Client soft deleted (deactivated)"
            );

            return NoContent();
        }
    }
}
