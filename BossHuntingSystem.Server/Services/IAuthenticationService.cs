using BossHuntingSystem.Server.Models;

namespace BossHuntingSystem.Server.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> AuthenticateAsync(LoginRequest request);
        bool ValidateToken(string token);
        string GenerateToken(string username);
    }
}
