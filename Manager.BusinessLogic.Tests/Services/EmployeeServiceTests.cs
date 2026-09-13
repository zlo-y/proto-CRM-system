using FluentAssertions;
using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.Exceptions;
using Manager.BusinessLogic.Services;
using Manager.BusinessLogic.Tests.TestHelpers;
using Manager.DataAccess.Entities;
using Manager.DataAccess.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Manager.BusinessLogic.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<Employee>> _employeesRepoMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly EmployeeService _sut;

    public EmployeeServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeesRepoMock = new Mock<IRepository<Employee>>();
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeesRepoMock.Object);

        _userManagerMock = MockUserManagerFactory.Create();

        _sut = new EmployeeService(_unitOfWorkMock.Object, _userManagerMock.Object);
    }

    [Fact]
    public async Task GetAllEmployeesAsync_NoSearch_ReturnsAllEmployees()
    {
        var user1 = new User { Id = 1, Email = "a@test.com" };
        var user2 = new User { Id = 2, Email = "b@test.com" };
        var employees = new List<Employee>
        {
            new() { Id = 1, Name = "Ivan", LastName = "Petrov", UserId = 1, User = user1 },
            new() { Id = 2, Name = "Anna", LastName = "Sidorova", UserId = 2, User = user2 }
        };

        _employeesRepoMock.Setup(r => r.GetAllWithIncludes(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, object?>>[]>()))
            .Returns(employees.AsAsyncQueryable());
        _employeesRepoMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<Employee>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<Employee> q, CancellationToken ct) => Task.FromResult(q.ToList()));

        var result = await _sut.GetAllEmployeesAsync("");

        result.Should().HaveCount(2);
        result.Select(e => e.Email).Should().BeEquivalentTo(new[] { "a@test.com", "b@test.com" });
    }

    [Fact]
    public async Task GetAllEmployeesAsync_WithSearch_FiltersMatchingEmployees()
    {
        var user1 = new User { Id = 1, Email = "a@test.com" };
        var user2 = new User { Id = 2, Email = "b@test.com" };
        var employees = new List<Employee>
        {
            new() { Id = 1, Name = "Ivan", LastName = "Petrov", UserId = 1, User = user1 },
            new() { Id = 2, Name = "Anna", LastName = "Sidorova", UserId = 2, User = user2 }
        };

        _employeesRepoMock.Setup(r => r.GetAllWithIncludes(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, object?>>[]>()))
            .Returns(employees.AsAsyncQueryable());
        _employeesRepoMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<Employee>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<Employee> q, CancellationToken ct) => Task.FromResult(q.ToList()));

        var result = await _sut.GetAllEmployeesAsync("ivan");

        result.Should().ContainSingle(e => e.Name == "Ivan");
    }

    [Fact]
    public async Task UpdateEmployeeAsync_EmployeeNotFound_ThrowsNotFoundException()
    {
        _employeesRepoMock
            .Setup(r => r.GetAllWithIncludes(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, object?>>[]>()))
            .Returns(new List<Employee>().AsAsyncQueryable());

        var dto = new EmployeeUpdateDto { Id = 999, Name = "X", LastName = "Y", Email = "x@test.com" };

        var act = async () => await _sut.UpdateEmployeeAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateEmployeeAsync_EmailUnchanged_DoesNotCallIdentity()
    {
        var user = new User { Id = 1, Email = "same@test.com" };
        var employee = new Employee { Id = 1, Name = "Old", LastName = "Name", UserId = 1, User = user };

        _employeesRepoMock
            .Setup(r => r.GetAllWithIncludes(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, object?>>[]>()))
            .Returns(new List<Employee> { employee }.AsAsyncQueryable());

        var dto = new EmployeeUpdateDto { Id = 1, Name = "New", LastName = "Name", Email = "same@test.com" };

        await _sut.UpdateEmployeeAsync(dto);

        employee.Name.Should().Be("New");
        _userManagerMock.Verify(m => m.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_EmailChanged_UpdatesIdentityEmailAndUserName()
    {
        var user = new User { Id = 1, Email = "old@test.com" };
        var employee = new Employee { Id = 1, Name = "Ivan", LastName = "Petrov", UserId = 1, User = user };

        _employeesRepoMock
            .Setup(r => r.GetAllWithIncludes(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, object?>>[]>()))
            .Returns(new List<Employee> { employee }.AsAsyncQueryable());

        _userManagerMock.Setup(m => m.SetEmailAsync(user, "new@test.com")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetUserNameAsync(user, "new@test.com")).ReturnsAsync(IdentityResult.Success);

        var dto = new EmployeeUpdateDto { Id = 1, Name = "Ivan", LastName = "Petrov", Email = "new@test.com" };

        await _sut.UpdateEmployeeAsync(dto);

        _userManagerMock.Verify(m => m.SetEmailAsync(user, "new@test.com"), Times.Once);
        _userManagerMock.Verify(m => m.SetUserNameAsync(user, "new@test.com"), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_IdentityEmailUpdateFails_ThrowsValidationAppException()
    {
        var user = new User { Id = 1, Email = "old@test.com" };
        var employee = new Employee { Id = 1, Name = "Ivan", LastName = "Petrov", UserId = 1, User = user };

        _employeesRepoMock
            .Setup(r => r.GetAllWithIncludes(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, object?>>[]>()))
            .Returns(new List<Employee> { employee }.AsAsyncQueryable());

        _userManagerMock
            .Setup(m => m.SetEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email is already taken" }));

        var dto = new EmployeeUpdateDto { Id = 1, Name = "Ivan", LastName = "Petrov", Email = "taken@test.com" };

        var act = async () => await _sut.UpdateEmployeeAsync(dto);

        await act.Should().ThrowAsync<ValidationAppException>();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_EmployeeNotFound_ReturnsFalse()
    {
        _employeesRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

        var result = await _sut.DeleteEmployeeAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEmployeeAsync_EmployeeExists_DeletesAndReturnsTrue()
    {
        var employee = new Employee { Id = 1, Name = "Ivan" };
        _employeesRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var result = await _sut.DeleteEmployeeAsync(1);

        result.Should().BeTrue();
        _employeesRepoMock.Verify(r => r.Delete(employee), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}