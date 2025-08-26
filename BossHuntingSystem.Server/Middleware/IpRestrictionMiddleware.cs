using BossHuntingSystem.Server.Models;
using Microsoft.Extensions.Options;

namespace BossHuntingSystem.Server.Middleware
{
    public class IpRestrictionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IpRestrictionsConfig _config;

        public IpRestrictionMiddleware(RequestDelegate next, IOptions<IpRestrictionsConfig> config)
        {
            _next = next;
            _config = config.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Skip IP restriction if not enabled
                if (!_config.Enabled)
                {
                    await _next(context);
                    return;
                }

                var requestPath = context.Request.Path.Value;
                var requestMethod = context.Request.Method;
                var endpointPattern = $"{requestMethod}:{requestPath}";

                // Skip IP restriction for debug endpoints
                if (requestPath?.Contains("/debug/") == true)
                {
                    await _next(context);
                    return;
                }

                // Check if this endpoint is restricted
                var isRestricted = _config.RestrictedEndpoints.Any(pattern => 
                    IsPatternMatch(endpointPattern, pattern));

                if (isRestricted)
                {
                    var clientIp = GetClientIpAddress(context);
                    
                    // Check if client IP is allowed
                    var isAllowed = _config.AllowedIps.Any(allowedIp => 
                        IsIpMatch(clientIp, allowedIp));

                    if (!isAllowed)
                    {
                        Console.WriteLine($"[IpRestriction] Access denied for IP {clientIp} to {endpointPattern}");
                        context.Response.StatusCode = 403; // Forbidden
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync($"{{\"error\":\"Access denied. Your IP address ({clientIp}) is not authorized to perform this action.\"}}");
                        return;
                    }

                    Console.WriteLine($"[IpRestriction] Access granted for IP {clientIp} to {endpointPattern}");
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IpRestriction] Error in middleware: {ex.Message}");
                // Continue with the request pipeline even if middleware fails
                await _next(context);
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded headers (for when behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsPatternMatch(string requestPattern, string configPattern)
        {
            // Simple wildcard matching
            if (configPattern.Contains("*"))
            {
                var regexPattern = configPattern
                    .Replace("*", ".*")
                    .Replace("?", "\\?");
                return System.Text.RegularExpressions.Regex.IsMatch(requestPattern, $"^{regexPattern}$");
            }

            return requestPattern.Equals(configPattern, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsIpMatch(string clientIp, string allowedIp)
        {
            // Handle IPv6 loopback
            if (allowedIp == "::1" && clientIp == "::1")
                return true;

            // Handle IPv4 loopback
            if (allowedIp == "127.0.0.1" && clientIp == "127.0.0.1")
                return true;

            // Handle localhost
            if (allowedIp == "127.0.0.1" && clientIp == "::1")
                return true;

            // Exact match
            return clientIp.Equals(allowedIp, StringComparison.OrdinalIgnoreCase);
        }
    }
}
