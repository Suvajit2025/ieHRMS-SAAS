using ieHRMS.Core.Interface;
using ieHRMS.Models;
using Jose;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using JwtSettings = ieHRMS.Models.JwtSettings;

namespace ieHRMS.Core.Repository
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public TokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        // For regular JWT (signed only)
        public string GenerateToken(Dictionary<string, object> claimsData, double expiryMinutes, bool isRefreshToken = false)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = claimsData.Select(kv => new Claim(kv.Key, kv.Value?.ToString() ?? string.Empty)).ToList();
            claims.Add(new Claim("token_type", isRefreshToken ? "refresh" : "access"));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // For encrypted JWE token (with AES-GCM)
        public string GenerateEncryptedToken(Dictionary<string, object> payload, double expireMinutes, bool isRefreshToken = false)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            payload["exp"] = DateTimeOffset.UtcNow.AddMinutes(expireMinutes).ToUnixTimeSeconds();
            payload["token_type"] = isRefreshToken ? "refresh" : "access";

            // Encode as JWE (encrypted token)
            var token = JWT.Encode(payload, key, JweAlgorithm.DIR, JweEncryption.A256GCM);
            return token;
        }
    }
}
