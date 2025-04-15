using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthService.Services;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IJwtTokenService? _jwtTokenService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration, IJwtTokenService? jwtTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // Step 1: Check if the user exists
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                    return Unauthorized("Invalid username or password.");

                // 🚨 TEMP DEBUG — check if PasswordHash is missing
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                    return BadRequest("User exists, but password hash is empty. Registration might be broken.");

                // Step 2: Validate the password
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                    return Unauthorized("Invalid username or password.");

                // Step 3: Generate JWT token
                var jwtToken = await _jwtTokenService.CreateToken(user);

                // Step 4: Generate and store Refresh Token
                var refreshTokenObj = _jwtTokenService.GenerateRefreshToken();
                user.RefreshToken = refreshTokenObj.Token;
                user.RefreshTokenExpiryTime = refreshTokenObj.Expires;

                await _userManager.UpdateAsync(user); // Save refresh token to DB

                // Step 5: Return the tokens to the client
                return Ok(new
                {
                    token = jwtToken,
                    refreshToken = refreshTokenObj.Token
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Login Error] {ex.Message} \n{ex.StackTrace}");
                // Optional: log ex here using ILogger
                return BadRequest("An error occurred during login. Please try again." + ex.Message + ex.StackTrace);
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
        {
            // 1. Extract user identity from expired access token
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(tokenModel.Token);

            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                return BadRequest("Invalid access token.");

            var username = principal.Identity.Name;

            // 2. Get the user from the database
            var user = await _userManager.FindByNameAsync(username);

            // 3. Check refresh token validity
            if (user == null ||
                user.RefreshToken != tokenModel.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid refresh token.");
            }

            // 4. Generate new tokens
            var newJwtToken = await _jwtTokenService.CreateToken(user);
            var newRefreshTokenObj = _jwtTokenService.GenerateRefreshToken();

            // 5. Save new refresh token to user record
            user.RefreshToken = newRefreshTokenObj.Token;
            user.RefreshTokenExpiryTime = newRefreshTokenObj.Expires;
            await _userManager.UpdateAsync(user);

            // 6. Return new tokens to client
            return Ok(new
            {
                token = newJwtToken,
                refreshToken = newRefreshTokenObj.Token
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("User not found.");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;

            await _userManager.UpdateAsync(user);
            return Ok("User logged out.");
        }

    }
}
