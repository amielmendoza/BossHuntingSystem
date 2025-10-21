using BossHuntingSystem.Server.Services;
using System.Net;

namespace BossHuntingSystem.Server.Middleware
{
    public class LicenseValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LicenseValidationMiddleware> _logger;

        // Paths that don't require license validation
        private static readonly string[] ExcludedPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/swagger",
            "/health"
        };

        public LicenseValidationMiddleware(RequestDelegate next, ILogger<LicenseValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
        {
            // Skip license validation for excluded paths
            if (ExcludedPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Skip if user is not authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            // Extract ClientId from context
            if (!context.Items.TryGetValue("ClientId", out var clientIdObj) || clientIdObj is not int clientId)
            {
                _logger.LogWarning("ClientId not found in context for authenticated request");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid client context" });
                return;
            }

            // Check if user is SuperAdmin (bypass license check)
            var userRole = context.Items["UserRole"] as string;
            if (userRole == "SuperAdmin")
            {
                await _next(context);
                return;
            }

            // Validate license
            var licenseStatus = await licenseService.ValidateLicenseAsync(clientId);

            if (!licenseStatus.IsValid)
            {
                _logger.LogWarning($"License validation failed for ClientId: {clientId}. Reason: {licenseStatus.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired; // 402
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "License expired",
                    message = licenseStatus.Message,
                    expirationDate = licenseStatus.ExpirationDate,
                    isExpired = licenseStatus.IsExpired
                });
                return;
            }

            // If in grace period, restrict to read-only operations
            if (licenseStatus.IsInGracePeriod)
            {
                var method = context.Request.Method;
                if (method != "GET" && method != "HEAD")
                {
                    _logger.LogWarning($"Write operation blocked for ClientId: {clientId} (Grace period - Read-only mode)");
                    context.Response.StatusCode = (int)HttpStatusCode.PaymentRequired; // 402
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "License in grace period",
                        message = "Your license has expired. Only read-only operations are allowed during the grace period. Please renew your license.",
                        gracePeriodEnd = licenseStatus.GracePeriodEnd,
                        daysRemaining = licenseStatus.DaysRemaining
                    });
                    return;
                }
            }

            // Add license info to response headers for frontend consumption
            context.Response.Headers["X-License-Days-Remaining"] = licenseStatus.DaysRemaining.ToString();
            context.Response.Headers["X-License-In-Grace-Period"] = licenseStatus.IsInGracePeriod.ToString();

            await _next(context);
        }
    }

    public static class LicenseValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseLicenseValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LicenseValidationMiddleware>();
        }
    }
}
