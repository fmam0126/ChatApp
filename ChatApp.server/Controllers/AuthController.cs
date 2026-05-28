using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace ChatApp.server.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("token")]
        public async Task<IActionResult> ReadToken()
        {
            var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return Unauthorized();
            }
            var token = authorizationHeader.Replace("Bearer ", "");

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);
            // Implementation for generating authentication token
            return Ok(new 
            {
                jwtToken.Header,
                Claims = jwtToken.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }
}
