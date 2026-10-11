using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryAPIProject.DTOs;
using InventoryAPIProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InventoryAPIProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userExists = await _userManager.FindByNameAsync(dto.Username);
            if (userExists != null)
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Username already exists!" });

            var emailExists = await _userManager.FindByEmailAsync(dto.Email);
            if (emailExists != null)
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Email already in use!" });

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Registration failed: {errors}" });
            }

            string roleName = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role;
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
            await _userManager.AddToRoleAsync(user, roleName);

            return Ok(new AuthResponseDto { IsSuccess = true, Message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return Unauthorized(new AuthResponseDto { IsSuccess = false, Message = "Invalid username or password!" });
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var (token, expiration) = GenerateJwtToken(user, userRoles);

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Login successful!",
                Token = token,
                Expiration = expiration,
                Username = user.UserName,
                Email = user.Email,
                Roles = userRoles.ToList()
            });
        }

        private (string token, DateTime expiration) GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtSecret = _configuration["Jwt:Key"] ?? "SuperSecretKeyForInventoryAPIProject2026SecureKey!";
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var mins) ? mins : 180;
            var expiration = DateTime.UtcNow.AddMinutes(durationMinutes);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "InventoryAPIProject",
                audience: _configuration["Jwt:Audience"] ?? "InventoryAPIProjectClient",
                expires: expiration,
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return (new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), expiration);
        }
    }
}
