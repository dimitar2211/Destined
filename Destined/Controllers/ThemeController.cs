using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using System.Threading.Tasks;

namespace Destined.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ThemeController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public ThemeController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
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
                await _signInManager.RefreshSignInAsync(user);
                return Ok(new { message = "Theme saved successfully." });
            }

            return StatusCode(500, "Failed to save theme.");
        }

        [HttpPost("UploadBackground")]
        public async Task<IActionResult> UploadBackground(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "backgrounds");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/backgrounds/{fileName}";

            // Update user claim
            var claims = await _userManager.GetClaimsAsync(user);
            var existingBgClaim = claims.FirstOrDefault(c => c.Type == "user_custom_background");

            if (existingBgClaim != null)
            {
                // Delete old file if it exists
                var oldFilePath = Path.Combine(_environment.WebRootPath, existingBgClaim.Value.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
                await _userManager.RemoveClaimAsync(user, existingBgClaim);
            }

            var result = await _userManager.AddClaimAsync(user, new Claim("user_custom_background", relativePath));
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return Ok(new { path = relativePath });
            }

            return StatusCode(500, "Failed to save custom background claim.");
        }
    }
}
