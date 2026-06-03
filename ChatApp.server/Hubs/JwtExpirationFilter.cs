using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatApp.server.Hubs
{
    public class JwtExpirationFilter : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext context,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            // Extract the expiration claim (exp) from the current connection user
            var expClaim = context.Context.User?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var expUnix))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

                // Check if the token has already expired
                if (DateTime.UtcNow > expirationTime)
                {
                    // Kick the user or throw an exception to block the message
                    context.Context.Abort(); // Abruptly terminates the connection
                    throw new HubException("Your session has expired. Please log in again.");
                }
            }

            return await next(context);
        }
    }
}