using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ArtcastaWebApi.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public TokenService(IConfiguration configuration, TokenValidationParameters tokenValidationParameters)
        {
            _config = configuration;
            _tokenValidationParameters = tokenValidationParameters;
        }
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken
               (
                   issuer: _config.GetSection("Jwt")["Issuer"],
                   audience: _config.GetSection("Jwt")["Audience"],
                   claims: claims,
                   expires: DateTime.UtcNow.Add(TimeSpan.Parse(_config.GetSection("Jwt")["TokenLifetime"])),
                   notBefore: DateTime.UtcNow,
                   signingCredentials: new SigningCredentials(
                       new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Jwt")["Key"])),
                       SecurityAlgorithms.HmacSha256)
               );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }

        }
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            var validationParameters = _tokenValidationParameters.Clone();
            validationParameters.ValidateLifetime = false;

            var principal = tokenHandler.ValidateToken(token, validationParameters, out securityToken);
            //_tokenValidationParameters.ValidateLifetime = true;
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }
    }
}
