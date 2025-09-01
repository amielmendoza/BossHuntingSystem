using BossHuntingSystem.Server.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BossHuntingSystem.Server.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _validTokens = new();

        public AuthenticationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                };
            }

            // Get users from configuration
            var users = _configuration.GetSection("Authentication:Users").Get<List<UserConfig>>();
            
            if (users == null || !users.Any())
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "No users configured in system"
                };
            }

            // Find matching user
            var user = users.FirstOrDefault(u => 
                string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(u.Password, request.Password, StringComparison.Ordinal));

            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Generate token
            var token = GenerateToken(user.Username);
            
            // Store token for validation
            _validTokens[token] = user.Username;

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                Username = user.Username
            };
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return _validTokens.ContainsKey(token);
        }

        public string GenerateToken(string username)
        {
            // Generate a simple token using timestamp and username
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var data = $"{username}:{timestamp}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        private class UserConfig
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
