using Microsoft.IdentityModel.Tokens;
using MRP.Models;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace MRP_Server.Services
{
    public class TokenManager
    {
        private const int DaysUntilExpiration = 7;
        private readonly string _RsaPath = Path.Combine(AppContext.BaseDirectory, "rsa_key.json");
        public RsaSecurityKey RsaSecurityKey { get; private set; }
        public string? Subject { get; private set; }
        public int? UserId { get; set; }

        public TokenManager()
        {
            RsaSecurityKey = GenerateOrLoadRsaKey();
        }

        public string GenerateJwtToken(string subject, int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, subject),
                    new Claim("uid", userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                ]),

                Expires = DateTime.UtcNow.AddDays(DaysUntilExpiration),
                Issuer = "MRP_Server",
                SigningCredentials = new SigningCredentials(RsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };
            var token = tokenHandler.CreateToken(descriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                IssuerSigningKey = RsaSecurityKey,
                ValidIssuer = "MRP_Server",
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = JwtRegisteredClaimNames.Sub
            };


            try
            {
                var principal = handler.ValidateToken(token, parameters, out _);

                Subject =
                    principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    principal.Identity?.Name;

                var uidClaim = principal.FindFirst("uid")?.Value;
                if (int.TryParse(uidClaim, out var id))
                    UserId = id;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JWT ERROR] {ex.Message}");
                return false;
            }
        }

        private RsaSecurityKey GenerateOrLoadRsaKey()
        {

            string keyFilePath = _RsaPath;
            if (File.Exists(keyFilePath))
            {
                try
                {
                    return LoadRsaKeyFromFile(keyFilePath);

                }
                catch (Exception)
                {
                    // HACK: no Exception handling yet
                }

            }
            RsaSecurityKey rsaSecurityKey;
            using (var provider = new RSACryptoServiceProvider(2048))
            {
                rsaSecurityKey = new RsaSecurityKey(provider.ExportParameters(true));
            }

            RsaSecurityKey = rsaSecurityKey;
            SaveRsaKeyToFile(rsaSecurityKey, keyFilePath);

            return rsaSecurityKey;

        }

        private void SaveRsaKeyToFile(RsaSecurityKey rsaSecurityKey, string filePath)
        {
            var obj = JsonConvert.SerializeObject(rsaSecurityKey.Parameters);

            File.WriteAllText(filePath, obj);
        }

        private RsaSecurityKey LoadRsaKeyFromFile(string filePath)
        {
            var param = JsonConvert.DeserializeObject<RSAParameters>(File.ReadAllText(filePath));

            RsaSecurityKey key = new RsaSecurityKey(param);

            return key;
        }
    }

}
