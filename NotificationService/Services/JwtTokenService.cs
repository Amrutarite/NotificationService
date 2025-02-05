using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NotificationService.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        // Constructor to inject IConfiguration
        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Method to generate JWT token
        public string GenerateJwtToken(string username)
        {
            // Validate username
            if (string.IsNullOrEmpty(username) || username != "testuser")
            {
                throw new UnauthorizedAccessException("Invalid username.");
            }

            // Read JwtSettings from environment variables or fallback to appsettings.json
            var jwtSettings = new JwtSettings
            {
                Issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? _configuration["JwtSettings:Issuer"],
                Audience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? _configuration["JwtSettings:Audience"],
                SecretKey = Environment.GetEnvironmentVariable("JwtSettings__SecretKey") ?? _configuration["JwtSettings:SecretKey"]
            };

            if (string.IsNullOrEmpty(jwtSettings.SecretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                // Add other claims as necessary
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // JwtSettings class for storing JWT configuration values
    public class JwtSettings
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecretKey { get; set; }
    }
}
