using InventoryAPIProject.Controllers;
using InventoryAPIProject.DTOs;
using InventoryAPIProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InventoryAPI.Tests
{
    public class AuthControllerTests
    {
        private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private Mock<RoleManager<IdentityRole>> CreateMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
        }

        private IConfiguration CreateTestConfig()
        {
            var mySettings = new Dictionary<string, string?>
            {
                { "Jwt:Key", "TestVerySecretSigningKeyForUnitTestingPurposesOnly123!" },
                { "Jwt:Issuer", "InventoryAPIProject" },
                { "Jwt:Audience", "InventoryAPIProjectClient" },
                { "Jwt:DurationInMinutes", "60" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(mySettings)
                .Build();
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
        {
            var mockUserManager = CreateMockUserManager();
            var mockRoleManager = CreateMockRoleManager();
            var config = CreateTestConfig();

            mockUserManager.Setup(m => m.FindByNameAsync("nonexistent"))
                .ReturnsAsync((ApplicationUser?)null);

            var controller = new AuthController(mockUserManager.Object, mockRoleManager.Object, config);
            var result = await controller.Login(new LoginDto { Username = "nonexistent", Password = "Password123" });

            var unauthResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<AuthResponseDto>(unauthResult.Value);
            Assert.False(response.IsSuccess);
        }

        [Fact]
        public async Task Login_ReturnsSuccessAndToken_WhenCredentialsAreValid()
        {
            var mockUserManager = CreateMockUserManager();
            var mockRoleManager = CreateMockRoleManager();
            var config = CreateTestConfig();

            var user = new ApplicationUser
            {
                Id = "user-1",
                UserName = "validuser",
                Email = "valid@example.com"
            };

            mockUserManager.Setup(m => m.FindByNameAsync("validuser"))
                .ReturnsAsync(user);
            mockUserManager.Setup(m => m.CheckPasswordAsync(user, "CorrectPassword!"))
                .ReturnsAsync(true);
            mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            var controller = new AuthController(mockUserManager.Object, mockRoleManager.Object, config);
            var result = await controller.Login(new LoginDto { Username = "validuser", Password = "CorrectPassword!" });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthResponseDto>(okResult.Value);
            Assert.True(response.IsSuccess);
            Assert.False(string.IsNullOrEmpty(response.Token));
            Assert.Equal("validuser", response.Username);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUsernameAlreadyExists()
        {
            var mockUserManager = CreateMockUserManager();
            var mockRoleManager = CreateMockRoleManager();
            var config = CreateTestConfig();

            mockUserManager.Setup(m => m.FindByNameAsync("existinguser"))
                .ReturnsAsync(new ApplicationUser { UserName = "existinguser" });

            var controller = new AuthController(mockUserManager.Object, mockRoleManager.Object, config);
            var dto = new RegisterDto { Username = "existinguser", Email = "new@example.com", Password = "Password123", FullName = "Existing User" };

            var result = await controller.Register(dto);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<AuthResponseDto>(badResult.Value);
            Assert.False(response.IsSuccess);
            Assert.Contains("Username already exists", response.Message);
        }
    }
}
