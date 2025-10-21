using System.Security.Claims;

namespace BossHuntingSystem.Server.Middleware
{
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract ClientId from JWT claims
            var clientIdClaim = context.User?.FindFirst("ClientId");

            if (clientIdClaim != null && int.TryParse(clientIdClaim.Value, out int clientId))
            {
                // Store ClientId in HttpContext.Items for easy access throughout the request pipeline
                context.Items["ClientId"] = clientId;
            }

            // Extract Username from JWT claims
            var usernameClaim = context.User?.FindFirst(ClaimTypes.Name);
            if (usernameClaim != null)
            {
                context.Items["Username"] = usernameClaim.Value;
            }

            // Extract Role from JWT claims
            var roleClaim = context.User?.FindFirst(ClaimTypes.Role);
            if (roleClaim != null)
            {
                context.Items["UserRole"] = roleClaim.Value;
            }

            await _next(context);
        }
    }

    public static class TenantContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantContextMiddleware>();
        }
    }

    // Helper class to access tenant context from anywhere in the app
    public interface ITenantContext
    {
        int? ClientId { get; }
        string? Username { get; }
        string? UserRole { get; }
    }

    public class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? ClientId
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("ClientId", out var clientId) == true)
                {
                    return clientId as int?;
                }
                return null;
            }
        }

        public string? Username
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("Username", out var username) == true)
                {
                    return username as string;
                }
                return null;
            }
        }

        public string? UserRole
        {
            get
            {
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("UserRole", out var role) == true)
                {
                    return role as string;
                }
                return null;
            }
        }
    }
}
