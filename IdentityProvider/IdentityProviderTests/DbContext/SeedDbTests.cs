using IdentityProvider.DbContext;
using IdentityProvider.Entities;
using IdentityProvider.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;

namespace IdentityProvider.Tests.DbContext
{
    public class SeedDbTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly SeedDb _sut;

        public SeedDbTests()
        {
            var userStore = new Mock<IUserStore<User>>();
            var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();

            _userManagerMock = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStore.Object, null!, null!, null!, null!);

            _sut = new SeedDb(_userManagerMock.Object, _roleManagerMock.Object);
        }

        #region Constructor

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var userStore = new Mock<IUserStore<User>>();
            var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
            var userManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!).Object;
            var roleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStore.Object, null!, null!, null!, null!).Object;

            // Act
            var result = new SeedDb(userManager, roleManager);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region SeedRoles

        [Fact]
        public async Task SeedRoles_WhenRolesExist_DoesNotCreateRoles()
        {
            // Arrange
            var existingRoles = new List<IdentityRole<Guid>>
            {
                new IdentityRole<Guid>(AppRoles.ClubAdmin)
            };
            var mockRolesQueryable = existingRoles.BuildMock();
            _roleManagerMock.Setup(x => x.Roles).Returns(mockRolesQueryable);

            // Act
            await _sut.SeedRoles();

            // Assert
            _roleManagerMock.Verify(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
        }

        [Fact]
        public async Task SeedRoles_WhenNoRolesExist_CreatesAllRoles()
        {
            // Arrange
            var emptyRoles = new List<IdentityRole<Guid>>();
            var mockRolesQueryable = emptyRoles.BuildMock();
            _roleManagerMock.Setup(x => x.Roles).Returns(mockRolesQueryable);
            _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _sut.SeedRoles();

            // Assert
            _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == AppRoles.ClubAdmin)), Times.Once);
            _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == AppRoles.ClubMember)), Times.Once);
            _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == AppRoles.PlatformAdmin)), Times.Once);
            _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == AppRoles.Visitor)), Times.Once);
            _roleManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityRole<Guid>>(r => r.Name == AppRoles.Chatbot)), Times.Once);
        }

        #endregion

        #region SeedAdminUser

        [Fact]
        public async Task SeedAdminUser_WhenUsersExist_ReturnsEarly()
        {
            // Arrange
            var existingUsers = new List<User>
            {
                User.Create("existing@test.com", "John", "Doe")
            };
            var mockUsersQueryable = existingUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            // Act
            await _sut.SeedAdminUser();

            // Assert
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task SeedAdminUser_WithValidJsonFile_CreatesUsersSuccessfully()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

            var jsonContent = @"[
                {
                    ""email"": ""admin@test.com"",
                    ""password"": ""Admin123!"",
                    ""confirmPassword"": ""Admin123!"",
                    ""firstName"": ""Admin"",
                    ""lastName"": ""User"",
                    ""role"": ""PlatformAdmin""
                }
            ]";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("hashed_password");
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            try
            {
                // Act
                await _sut.SeedAdminUser();

                // Assert
                _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.Email == "admin@test.com")), Times.Once);
                _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "PlatformAdmin"), Times.Once);
                passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<User>(), "Admin123!"), Times.Once);
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        [Fact]
        public async Task SeedAdminUser_WithEmptyJsonArray_ThrowsRegistrationFailedException()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var jsonContent = "[]";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<RegistrationFailedException>(() => _sut.SeedAdminUser());
                Assert.Contains("No user data found", exception.Message);
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        [Fact]
        public async Task SeedAdminUser_WithNullJson_ThrowsRegistrationFailedException()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var jsonContent = "null";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<RegistrationFailedException>(() => _sut.SeedAdminUser());
                Assert.Contains("No user data found", exception.Message);
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        [Fact]
        public async Task SeedAdminUser_WithInvalidJson_ThrowsRegistrationFailedException()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var jsonContent = "{ invalid json }";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<RegistrationFailedException>(() => _sut.SeedAdminUser());
                Assert.Contains("Failed to parse SeedData.json", exception.Message);
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        [Fact]
        public async Task SeedAdminUser_WhenUserCreationFails_ThrowsRegistrationFailedException()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

            var jsonContent = @"[
                {
                    ""email"": ""admin@test.com"",
                    ""password"": ""Admin123!"",
                    ""confirmPassword"": ""Admin123!"",
                    ""firstName"": ""Admin"",
                    ""lastName"": ""User"",
                    ""role"": ""PlatformAdmin""
                }
            ]";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("hashed_password");

            var errors = new[]
            {
                new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" }
            };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Failed(errors));

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<RegistrationFailedException>(() => _sut.SeedAdminUser());
                Assert.Contains("Email already exists", exception.Message);
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        [Fact]
        public async Task SeedAdminUser_WhenRoleAssignmentFails_ThrowsRegistrationFailedException()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

            var jsonContent = @"[
                {
                    ""email"": ""admin@test.com"",
                    ""password"": ""Admin123!"",
                    ""confirmPassword"": ""Admin123!"",
                    ""firstName"": ""Admin"",
                    ""lastName"": ""User"",
                    ""role"": ""InvalidRole""
                }
            ]";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("hashed_password");
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            var errors = new[]
            {
                new IdentityError { Code = "RoleNotFound", Description = "Role does not exist" }
            };
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(errors));

            try
            {
                // Act & Assert
                var exception = await Assert.ThrowsAsync<RegistrationFailedException>(() => _sut.SeedAdminUser());
                Assert.Contains("Role does not exist", exception.Message);
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        [Fact]
        public async Task SeedAdminUser_WithMultipleUsers_CreatesAllUsers()
        {
            // Arrange
            var emptyUsers = new List<User>();
            var mockUsersQueryable = emptyUsers.BuildMock();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsersQueryable);

            var passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _userManagerMock.Object.PasswordHasher = passwordHasherMock.Object;

            var jsonContent = @"[
                {
                    ""email"": ""admin1@test.com"",
                    ""password"": ""Admin123!"",
                    ""confirmPassword"": ""Admin123!"",
                    ""firstName"": ""Admin"",
                    ""lastName"": ""One"",
                    ""role"": ""PlatformAdmin""
                },
                {
                    ""email"": ""admin2@test.com"",
                    ""password"": ""Admin456!"",
                    ""confirmPassword"": ""Admin456!"",
                    ""firstName"": ""Admin"",
                    ""lastName"": ""Two"",
                    ""role"": ""ClubAdmin""
                }
            ]";

            Directory.CreateDirectory("DbContext");
            await File.WriteAllTextAsync("DbContext/SeedData.json", jsonContent);

            passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("hashed_password");
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            try
            {
                // Act
                await _sut.SeedAdminUser();

                // Assert
                _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.Email == "admin1@test.com")), Times.Once);
                _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.Email == "admin2@test.com")), Times.Once);
                _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Exactly(2));
            }
            finally
            {
                if (File.Exists("DbContext/SeedData.json"))
                    File.Delete("DbContext/SeedData.json");
                if (Directory.Exists("DbContext"))
                    Directory.Delete("DbContext");
            }
        }

        #endregion
    }
}
