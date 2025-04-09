using AuthService.Models;
using System.Security.Claims;

namespace AuthService.Services
{
    public interface IJwtTokenService
    {
        Task<string> CreateToken(ApplicationUser user);
        RefreshToken GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
