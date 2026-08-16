using Amazon.S3;
using Dotnet_test1_authentication_authorization_with_product.Configuration;
using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Models;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Dotnet_test1_authentication_authorization_with_product.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IR2ImageService _r2ImageService;
        private readonly ILogger<ProfileController> _logger;
        private readonly R2Storage _r2Options;

        public ProfileController(
            UserDbContext context,
            IR2ImageService r2ImageService,
            ILogger<ProfileController> logger,
            IOptions<R2Storage> r2Options)
        {
            _context = context;
            _r2ImageService = r2ImageService;
            _logger = logger;
            _r2Options = r2Options.Value;
        }

        // GET: api/profile/avatar
        [Authorize]
        [HttpGet("avatar")]
        public async Task<IActionResult> GetAvatar()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { error = "Invalid user ID token" });

                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null || string.IsNullOrEmpty(profile.ImageUrl))
                    return NotFound(new { error = "Profile or avatar image not found" });

                // Handle external Google OAuth links directly
                if (profile.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                   !profile.ImageUrl.Contains(_r2Options.PublicUrl))
                {
                    return Redirect(profile.ImageUrl);
                }

                // R2ImageService extracts the object key internally
                var r2Response = await _r2ImageService.GetImageAsync(profile.ImageUrl);

                if (r2Response?.Stream == null || r2Response.Stream.Length == 0)
                    return NotFound(new { error = "Image stream is empty" });

                return File(r2Response.Stream, r2Response.ContentType);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = "Image file not found in storage" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetAvatar error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: api/profile
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ProfileDto>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null)
                {
                    return NotFound("Profile not found. Please create one.");
                }

                return Ok(new ProfileDto
                {
                    Id = profile.Id,
                    Email = profile.Email,
                    ImageUrl = profile.ImageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetProfile error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/profile
        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProfileDto>> CreateProfile([FromForm] CreateProfileDto profile)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                if (await _context.Profiles.AnyAsync(item => item.UserId == userId))
                {
                    return BadRequest("A profile already exists for this account.");
                }

                if (await _context.Profiles.AnyAsync(item => item.Email == profile.Email))
                {
                    return BadRequest("Email already exists.");
                }

                if (profile.Image == null || profile.Image.Length == 0)
                {
                    return BadRequest("Please upload a profile image.");
                }

                string uploadedImageUrl = await _r2ImageService.UploadImageAsync(profile.Image, folder: "avatars");

                var userProfile = new Profile
                {
                    UserId = userId,
                    ImageUrl = uploadedImageUrl,
                    Email = profile.Email
                };

                _context.Profiles.Add(userProfile);
                await _context.SaveChangesAsync();

                return Ok(new ProfileDto
                {
                    Id = userProfile.Id,
                    Email = userProfile.Email,
                    ImageUrl = userProfile.ImageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateProfile error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/profile
        [Authorize]
        [HttpPut]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProfileDto>> UpdateProfile([FromForm] CreateProfileDto profile)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var existingProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (existingProfile == null)
                {
                    return NotFound("Profile not found.");
                }

                if (!string.IsNullOrEmpty(profile.Email) &&
                    await _context.Profiles.AnyAsync(p => p.Email == profile.Email && p.UserId != userId))
                {
                    return BadRequest("Email already exists for another profile.");
                }

                string uploadedImageUrl = existingProfile.ImageUrl;

                if (profile.Image != null && profile.Image.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingProfile.ImageUrl))
                    {
                        try
                        {
                            // Pass the stored URL directly; R2ImageService cleans the key
                            await _r2ImageService.DeleteImageAsync(existingProfile.ImageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to delete old image: {ex.Message}");
                        }
                    }

                    uploadedImageUrl = await _r2ImageService.UploadImageAsync(profile.Image, folder: "avatars");
                }

                existingProfile.Email = profile.Email ?? existingProfile.Email;
                existingProfile.ImageUrl = uploadedImageUrl;

                await _context.SaveChangesAsync();

                return Ok(new ProfileDto
                {
                    Id = existingProfile.Id,
                    Email = existingProfile.Email,
                    ImageUrl = existingProfile.ImageUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateProfile error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/profile
        [Authorize]
        [HttpDelete]
        public async Task<ActionResult> DeleteProfile()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null)
                {
                    return NotFound("Profile not found.");
                }

                if (!string.IsNullOrEmpty(profile.ImageUrl))
                {
                    try
                    {
                        // Pass stored URL directly
                        await _r2ImageService.DeleteImageAsync(profile.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete image from Cloudflare: {ex.Message}");
                    }
                }

                _context.Profiles.Remove(profile);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Profile deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteProfile error: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}















