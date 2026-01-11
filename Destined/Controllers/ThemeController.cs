using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Destined.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ThemeController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ThemeController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("SetTheme")]
        public async Task<IActionResult> SetTheme([FromBody] string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
                return BadRequest("Theme name is required.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var claims = await _userManager.GetClaimsAsync(user);
            var existingThemeClaim = claims.FirstOrDefault(c => c.Type == "user_theme");

            if (existingThemeClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, existingThemeClaim);
            }

            var result = await _userManager.AddClaimAsync(user, new Claim("user_theme", themeName));
            if (result.Succeeded)
            {
                // Refresh the sign-in cookie so the new claim is available immediately
                await _signInManager.RefreshSignInAsync(user);
                return Ok(new { message = "Theme saved successfully." });
            }

            return StatusCode(500, "Failed to save theme.");
        }
    }
}
