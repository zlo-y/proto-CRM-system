using FluentAssertions;
using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.Exceptions;
using Manager.BusinessLogic.Interfaces;
using Manager.BusinessLogic.Services;
using Manager.BusinessLogic.Tests.TestHelpers;
using Manager.DataAccess.Entities;
using Manager.DataAccess.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;


namespace Manager.BusinessLogic.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<Employee>> _employeesRepoMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userManagerMock = MockUserManagerFactory.Create();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeesRepoMock = new Mock<IRepository<Employee>>();
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeesRepoMock.Object);

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("test-super-secret-key-for-unit-tests-only-32chars!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _emailSenderMock = new Mock<IEmailSender>();

        _sut = new AuthService(_userManagerMock.Object, _configurationMock.Object, _unitOfWorkMock.Object, _emailSenderMock.Object);
    }

// LoginAsync

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsAuthenticationFailedException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var dto = new LoginDto { Email = "ghost@test.com", Password = "whatever123" };

        var act = async () => await _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsSameExceptionAsUserNotFound()
    {
        var user = new User { Id = 1, Email = "real@test.com", UserName = "real@test.com" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var dto = new LoginDto { Email = user.Email, Password = "wrong-password" };

        var act = async () => await _sut.LoginAsync(dto);

        var exception = await act.Should().ThrowAsync<AuthenticationFailedException>();
        exception.Which.Message.Should().Be("Неверный email или пароль");
    }

    [Fact]
    public async Task LoginAsync_AccountLockedOut_DoesNotEvenCheckPassword()
    {
        var user = new User { Id = 1, Email = "locked@test.com" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var dto = new LoginDto { Email = user.Email, Password = "any-password" };

        var act = async () => await _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<AuthenticationFailedException>();

        _userManagerMock.Verify(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_NoLinkedEmployee_ThrowsNotFoundException()
    {
        var user = new User { Id = 1, Email = "orphan@test.com", UserName = "orphan@test.com" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        _employeesRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee>().AsAsyncQueryable());

        var dto = new LoginDto { Email = user.Email, Password = "correct-password" };

        var act = async () => await _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndResetsFailedCount()
    {
        var user = new User { Id = 1, Email = "valid@test.com", UserName = "valid@test.com" };
        var employee = new Employee { Id = 10, UserId = 1, Name = "Ivan", LastName = "Ivanov" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct-password")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employee" });

        _employeesRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee> { employee }.AsAsyncQueryable());

        var dto = new LoginDto { Email = user.Email, Password = "correct-password" };

        var result = await _sut.LoginAsync(dto);

        result.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(user.Email);
        _userManagerMock.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

// RegisterAsync

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsConflictException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User());

        var dto = new RegisterDto { Email = "taken@test.com", Password = "Password123", FirstName = "A", LastName = "B" };

        var act = async () => await _sut.RegisterAsync(dto);

        await act.Should().ThrowAsync<ConflictException>();

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_IdentityCreateFails_RollsBackTransaction()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Weak password" }));

        var dto = new RegisterDto { Email = "new@test.com", Password = "weak", FirstName = "A", LastName = "B" };

        var act = async () => await _sut.RegisterAsync(dto);

        await act.Should().ThrowAsync<ValidationAppException>();

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_Success_CommitsTransactionAndReturnsToken()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((u, p) => u.Id = 1)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), "Employee"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "Employee" });

        _employeesRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Callback<Employee, CancellationToken>((e, ct) => e.Id = 100)
            .Returns(Task.CompletedTask);

        var dto = new RegisterDto { Email = "new@test.com", Password = "StrongPass123", FirstName = "Ivan", LastName = "Ivanov" };

        var result = await _sut.RegisterAsync(dto);

        result.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(dto.Email);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

// ForgotPasswordAsync

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_DoesNotThrowAndDoesNotSendEmail()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns("http://localhost:5173");

        var dto = new ForgotPasswordDto { Email = "unknown@test.com" };

        await _sut.ForgotPasswordAsync(dto);

        _emailSenderMock.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_KnownEmail_SendsResetEmail()
    {
        var user = new User { Id = 1, Email = "known@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-123");
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns("http://localhost:5173");

        var dto = new ForgotPasswordDto { Email = user.Email };

        await _sut.ForgotPasswordAsync(dto);

        _emailSenderMock.Verify(
            e => e.SendPasswordResetEmailAsync(user.Email, It.Is<string>(link => link.Contains("reset-token-123")), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}