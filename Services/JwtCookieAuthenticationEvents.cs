using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;

namespace Hospital.Services
{
    /// <summary>
    /// Custom cookie authentication events that validates JWT tokens stored in cookies
    /// </summary>
    public class JwtCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtCookieAuthenticationEvents> _logger;

        public JwtCookieAuthenticationEvents(IConfiguration configuration, ILogger<JwtCookieAuthenticationEvents> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // Always try to get and validate JWT token from cookie
        var jwtToken = context.Request.Cookies["jwt_token"];

        if (string.IsNullOrEmpty(jwtToken))
        {
            _logger.LogWarning("JWT token cookie not found - rejecting principal");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("jwt_token");
            return;
        }

        try
        {
            // Validate JWT token
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key is not configured"));
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidAudience = _configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero,
                // Map role claim correctly
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };

            // Validate token and get principal
            var jwtPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out SecurityToken validatedToken);
            
            // Extract claims from JWT
            var claims = jwtPrincipal.Claims.ToList();
            
            // Create a new claims identity with the validated JWT claims
            var claimsIdentity = new ClaimsIdentity(
                claims, 
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role);
            
            // Replace the principal with the validated JWT principal
            context.ReplacePrincipal(new ClaimsPrincipal(claimsIdentity));
            context.ShouldRenew = true; // Renew the cookie to extend expiration

            _logger.LogDebug("JWT token validated successfully for user: {User}", 
                claimsIdentity.Name);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("JWT token has expired: {Message}", ex.Message);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("jwt_token");
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("JWT token signature is invalid: {Message}", ex.Message);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("jwt_token");
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning("JWT token validation failed: {Message}", ex.Message);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("jwt_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token from cookie");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Cookies.Delete("jwt_token");
        }
    }
    }
}
