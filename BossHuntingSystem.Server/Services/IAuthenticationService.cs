using BossHuntingSystem.Server.Models;
using System.Security.Claims;

namespace BossHuntingSystem.Server.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> AuthenticateAsync(LoginRequest request);
        bool ValidateToken(string token);
        ClaimsPrincipal? GetUserFromToken(string token);
    }
}
