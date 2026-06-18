using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChatApp.server.Class;
using ChatApp.server.Models;

namespace ChatApp.Server.Tests.Services;

public class TokenServiceTests
{
    private const string SecretKey = "TestSecretKey-1234567890-ABCDEFGHIJKLMNOP!";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";
    private const int ExpiresMinutes = 30;

    private readonly TokenService _service;

    public TokenServiceTests()
    {
        _service = new TokenService(SecretKey, Issuer, Audience, ExpiresMinutes);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var user = new User { Id = 1, Name = "cena" };

        var token = _service.GenerateToken(user);

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateToken_ProducesValidJwt()
    {
        var user = new User { Id = 42, Name = "bob" };

        var token = _service.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var validatedToken = handler.ReadJwtToken(token);

        Assert.NotNull(validatedToken);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        var user = new User { Id = 99, Name = "charlie" };

        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName);

        Assert.NotNull(subClaim);
        Assert.Equal("99", subClaim!.Value);
        Assert.NotNull(nameClaim);
        Assert.Equal("charlie", nameClaim!.Value);
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        var user = new User { Id = 1, Name = "dave" };

        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_HasFutureExpiration()
    {
        var user = new User { Id = 1, Name = "eve" };

        var token = _service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ProduceDifferentTokens()
    {
        var cena = new User { Id = 1, Name = "cena" };
        var bob = new User { Id = 2, Name = "bob" };

        var token1 = _service.GenerateToken(cena);
        var token2 = _service.GenerateToken(bob);

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_SameUser_ProducesSameClaimsButDifferentIat()
    {
        var user = new User { Id = 1, Name = "frank" };

        var token1 = _service.GenerateToken(user);
        // Small delay to ensure different iat claim
        Thread.Sleep(1100);
        var token2 = _service.GenerateToken(user);

        // Tokens differ because iat (issued-at) claim changes across seconds
        Assert.NotEqual(token1, token2);

        // But the sub and name claims remain the same
        var handler = new JwtSecurityTokenHandler();
        var jwt1 = handler.ReadJwtToken(token1);
        var jwt2 = handler.ReadJwtToken(token2);

        Assert.Equal(
            jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value,
            jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(
            jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value,
            jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
    }
}
