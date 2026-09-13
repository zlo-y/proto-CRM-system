using FluentAssertions;
using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.Exceptions;
using Manager.BusinessLogic.Interfaces;
using Manager.BusinessLogic.Models;
using Manager.BusinessLogic.Services;
using Manager.BusinessLogic.Tests.TestHelpers;
using Manager.DataAccess.Entities;
using Manager.DataAccess.Interfaces;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Manager.BusinessLogic.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<Project>> _projectsRepoMock;
    private readonly Mock<IRepository<Employee>> _employeesRepoMock;
    private readonly Mock<IRepository<ProjectEmployee>> _projectEmployeesRepoMock;
    private readonly Mock<IRepository<ProjectDocument>> _projectDocumentsRepoMock;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _projectsRepoMock = new Mock<IRepository<Project>>();
        _employeesRepoMock = new Mock<IRepository<Employee>>();
        _projectEmployeesRepoMock = new Mock<IRepository<ProjectEmployee>>();
        _projectDocumentsRepoMock = new Mock<IRepository<ProjectDocument>>();

        _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectsRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeesRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ProjectEmployees).Returns(_projectEmployeesRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ProjectDocuments).Returns(_projectDocumentsRepoMock.Object);

        _fileStorageMock = new Mock<IFileStorageService>();

        var fileSettings = Options.Create(new FileSettings { MaxFileSize = 5_000_000, AllowedExtensions = ".pdf,.docx" });

        _sut = new ProjectService(_unitOfWorkMock.Object, fileSettings, _fileStorageMock.Object);
    }

// CreateProjectAsync 

    [Fact]
    public async Task CreateProjectAsync_ManagerDoesNotExist_ThrowsArgumentException()
    {
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new ProjectServiceDto
        {
            Name = "Test", Customer = "C", Executor = "E",
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(5),
            Priority = 1, ManagerId = 999
        };

        var act = async () => await _sut.CreateProjectAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
        _projectsRepoMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProjectAsync_StartDateAfterEndDate_ThrowsArgumentException()
    {
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ProjectServiceDto
        {
            Name = "Test", Customer = "C", Executor = "E",
            StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today,
            Priority = 1, ManagerId = 1
        };

        var act = async () => await _sut.CreateProjectAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateProjectAsync_ValidData_PersistsProjectWithCorrectFields()
    {
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Project? captured = null;
        _projectsRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, ct) => captured = p)
            .Returns(Task.CompletedTask);

        var dto = new ProjectServiceDto
        {
            Name = "New Project", Customer = "Acme", Executor = "Team A",
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(30),
            Priority = 2, ManagerId = 5
        };

        await _sut.CreateProjectAsync(dto);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("New Project");
        captured.ManagerId.Should().Be(5);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProjectAsync_WithEmployeeIds_LinksThemToProject()
    {
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Project? captured = null;
        _projectsRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, ct) => captured = p)
            .Returns(Task.CompletedTask);

        var dto = new ProjectServiceDto
        {
            Name = "Team Project", Customer = "C", Executor = "E",
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(5),
            Priority = 1, ManagerId = 1,
            ExecutorId = new List<int> { 10, 20, 30 }
        };

        await _sut.CreateProjectAsync(dto);

        captured!.ProjectEmployees.Should().HaveCount(3);
        captured.ProjectEmployees.Select(pe => pe.EmployeeId).Should().BeEquivalentTo(new[] { 10, 20, 30 });
    }

//  UpdateProjectAsync 

    [Fact]
    public async Task UpdateProjectAsync_ProjectNotFound_ThrowsArgumentException()
    {
        _projectsRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var dto = new ProjectUpdateDto { Id = 999, Name = "X", Customer = "X", Executor = "X", Priority = 1 };

        var act = async () => await _sut.UpdateProjectAsync(dto, currentEmployeeId: 1, isAdmin: false);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateProjectAsync_NotOwnerAndNotAdmin_ThrowsForbiddenAndDoesNotSave()
    {
        var project = new Project { Id = 5, ManagerId = 1, Name = "Original" };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var dto = new ProjectUpdateDto { Id = 5, Name = "Hacked", Customer = "X", Executor = "Y", Priority = 1 };

        var act = async () => await _sut.UpdateProjectAsync(dto, currentEmployeeId: 999, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
        project.Name.Should().Be("Original");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProjectAsync_IsAdmin_AllowsUpdateEvenIfNotOwner()
    {
        var project = new Project { Id = 5, ManagerId = 1, Name = "Original" };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var dto = new ProjectUpdateDto { Id = 5, Name = "Updated by Admin", Customer = "X", Executor = "Y", Priority = 1 };

        await _sut.UpdateProjectAsync(dto, currentEmployeeId: 999, isAdmin: true);

        project.Name.Should().Be("Updated by Admin");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectAsync_Owner_AllowsUpdate()
    {
        var project = new Project { Id = 5, ManagerId = 42, Name = "Original" };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var dto = new ProjectUpdateDto { Id = 5, Name = "Updated by Owner", Customer = "X", Executor = "Y", Priority = 1 };

        await _sut.UpdateProjectAsync(dto, currentEmployeeId: 42, isAdmin: false);

        project.Name.Should().Be("Updated by Owner");
    }

//DeleteProjectAsync 

[Fact]
public async Task DeleteProjectAsync_ProjectNotFound_ThrowsNotFoundException()
{
    _projectsRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

    var act = async () => await _sut.DeleteProjectAsync(999, currentEmployeeId: 1, isAdmin: true);

    await act.Should().ThrowAsync<NotFoundException>();
}

    [Fact]
    public async Task DeleteProjectAsync_NotOwnerAndNotAdmin_ThrowsForbidden()
    {
        var project = new Project { Id = 5, ManagerId = 1 };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = async () => await _sut.DeleteProjectAsync(5, currentEmployeeId: 999, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
        _projectsRepoMock.Verify(r => r.Delete(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProjectAsync_Success_DeletesAssociatedFilesFromDisk()
    {
        var project = new Project { Id = 5, ManagerId = 42 };
        var documents = new List<ProjectDocument>
        {
            new() { Id = 1, ProjectId = 5, FilePath = "uploads/file1.pdf" },
            new() { Id = 2, ProjectId = 5, FilePath = "uploads/file2.pdf" }
        };

        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _projectDocumentsRepoMock.Setup(r => r.GetAll()).Returns(documents.AsAsyncQueryable());
        _projectDocumentsRepoMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<ProjectDocument>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<ProjectDocument> q, CancellationToken ct) => Task.FromResult(q.ToList()));

        var result = await _sut.DeleteProjectAsync(5, currentEmployeeId: 42, isAdmin: false);

        result.Should().BeTrue();
        _fileStorageMock.Verify(f => f.DeleteFile("uploads/file1.pdf"), Times.Once);
        _fileStorageMock.Verify(f => f.DeleteFile("uploads/file2.pdf"), Times.Once);
    }

//AddEmployeeProject 

    [Fact]
    public async Task AddEmployeeProject_ProjectNotFound_ThrowsArgumentException()
    {
        _projectsRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        var act = async () => await _sut.AddEmployeeProject(999, employeeId: 1, currentEmployeeId: 1, isAdmin: false);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddEmployeeProject_NotOwnerAndNotAdmin_ThrowsForbidden()
    {
        var project = new Project { Id = 5, ManagerId = 1 };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = async () => await _sut.AddEmployeeProject(5, employeeId: 10, currentEmployeeId: 999, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

   [Fact]
    public async Task AddEmployeeProject_LinkAlreadyExists_ThrowsValidationAppException()
   {
    var project = new Project { Id = 5, ManagerId = 42 };
    _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);
    _employeesRepoMock
        .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);
    _projectEmployeesRepoMock
        .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<ProjectEmployee, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true); // связь уже есть

           var act = async () => await _sut.AddEmployeeProject(5, employeeId: 10, currentEmployeeId: 42, isAdmin: false);

          await act.Should().ThrowAsync<ValidationAppException>();
         _projectEmployeesRepoMock.Verify(r => r.AddAsync(It.IsAny<ProjectEmployee>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task AddEmployeeProject_ValidNewLink_AddsAndSaves()
    {
        var project = new Project { Id = 5, ManagerId = 42 };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _projectEmployeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<ProjectEmployee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.AddEmployeeProject(5, employeeId: 10, currentEmployeeId: 42, isAdmin: false);

        _projectEmployeesRepoMock.Verify(r => r.AddAsync(It.IsAny<ProjectEmployee>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

//RemoveEmployeeFromProjectAsync 

    [Fact]
    public async Task RemoveEmployeeFromProjectAsync_NotOwnerAndNotAdmin_ThrowsForbidden()
    {
        var project = new Project { Id = 5, ManagerId = 1 };
        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = async () => await _sut.RemoveEmployeeFromProjectAsync(5, employeeId: 10, currentEmployeeId: 999, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RemoveEmployeeFromProjectAsync_LinkExists_DeletesIt()
    {
        var project = new Project { Id = 5, ManagerId = 42 };
        var link = new ProjectEmployee { ProjectId = 5, EmployeeId = 10 };

        _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _projectEmployeesRepoMock.Setup(r => r.GetAll()).Returns(new List<ProjectEmployee> { link }.AsAsyncQueryable());
        _projectEmployeesRepoMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<ProjectEmployee>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<ProjectEmployee> q, CancellationToken ct) => Task.FromResult(q.ToList()));

        await _sut.RemoveEmployeeFromProjectAsync(5, employeeId: 10, currentEmployeeId: 42, isAdmin: false);

        _projectEmployeesRepoMock.Verify(r => r.Delete(link), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEmployeeFromProjectAsync_LinkDoesNotExist_ThrowsNotFoundException()
    {
    var project = new Project { Id = 5, ManagerId = 42 };

    _projectsRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(project);
    _projectEmployeesRepoMock.Setup(r => r.GetAll()).Returns(new List<ProjectEmployee>().AsAsyncQueryable());
    _projectEmployeesRepoMock
        .Setup(r => r.ToListAsync(It.IsAny<IQueryable<ProjectEmployee>>(), It.IsAny<CancellationToken>()))
        .Returns((IQueryable<ProjectEmployee> q, CancellationToken ct) => Task.FromResult(q.ToList()));

    var act = async () => await _sut.RemoveEmployeeFromProjectAsync(5, employeeId: 999, currentEmployeeId: 42, isAdmin: false);

    await act.Should().ThrowAsync<NotFoundException>();
    _projectEmployeesRepoMock.Verify(r => r.Delete(It.IsAny<ProjectEmployee>()), Times.Never);
    }
}