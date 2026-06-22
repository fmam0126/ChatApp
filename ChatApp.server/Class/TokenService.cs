using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatApp.server.Models;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.server.Class;

public class TokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expires;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="secretKey">The secret key for signing tokens.</param>
    /// <param name="issuer">The issuer of the tokens.</param>
    /// <param name="audience">The audience of the tokens.</param>
    /// <param name="expires">The expiration time of the tokens in minutes.</param>
    public TokenService(string secretKey, string issuer, string audience, int expires)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _expires = expires;
    }
    /// <summary>
    /// Generates a JWT token for the given user. The token includes claims for the user's ID and name, and is signed using the configured secret key. The token is valid for the configured expiration time.
    /// </summary>
    /// <param name="user">The user for whom to generate a token</param>
    /// <returns>The generated JWT token</returns>
    public string GenerateToken(User user)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Name),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expires),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
