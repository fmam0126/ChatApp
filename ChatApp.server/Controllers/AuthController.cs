using ChatApp.server.Class;
using ChatApp.server.DTO;
using ChatApp.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.server.Controllers
{
    [Route("auth")]
    [ApiController]
    [EnableRateLimiting("LoginPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly ChatContext _context;
        private readonly TokenService _tokenService;
        private readonly ConnectedUsersService _connectedUsers;

        public AuthController(ChatContext context, TokenService tokenService, ConnectedUsersService connectedUsers)
        {
            _context = context;
            _tokenService = tokenService;
            _connectedUsers = connectedUsers;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var username = request.Username?.Trim();
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 30)
            {
                return BadRequest(new { message = "Username must be between 3 and 30 characters." });
            }

            // Check if username is already taken by an active connection
            if (_connectedUsers.IsUsernameTaken(username))
            {
                return Conflict(new { message = "Username already taken. Please choose another." });
            }

            // Find or create user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Name == username);
            if (user is null)
            {
                user = new User { Name = username };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = _tokenService.GenerateToken(user);

            return Ok(new LoginResponseDTO
            {
                Token = token,
                Username = user.Name,
                UserId = user.Id
            });
        }
    }
}
