using ChatApp.server.Class;
using ChatApp.server.DTO;
using ChatApp.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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
        /// <summary>
        /// Handles user login requests. Validates the provided username, checks for active connections with the same username, and either finds or creates a user in the database. 
        /// If successful, generates a JWT token for the user and returns it along with the username and user ID. 
        /// If the username is invalid or already taken by an active connection, returns an appropriate error response.
        /// </summary>
        /// <param name="request">The login request containing the username.</param>
        /// <returns>The result of the login operation.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var username = request.Username?.Trim();
            var password = request.Password;
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 30)
            {
                return BadRequest(new { message = "Username must be between 3 and 30 characters." });
            }
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8 )
            {
                return BadRequest(new { message = "Password must be at least 8 characters"});
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
                user = new User
                { 
                    Name = username,
                    PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 11)
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // verify password 
            if (BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash)){
                var token = _tokenService.GenerateToken(user);

                return Ok(new LoginResponseDTO
                {
                    Token = token,
                    Username = user.Name,
                    UserId = user.Id
                });
            }
            return Conflict(new { message = "Username or password is incorrect" });

        }
    }
}
