using FluentAssertions;
using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.Exceptions;
using Manager.BusinessLogic.Interfaces;
using Manager.BusinessLogic.Services;
using Manager.DataAccess.Entities;
using Manager.DataAccess.Interfaces;
using Moq;
using System.Linq.Expressions;
using TaskStatus = Manager.DataAccess.Enums.TaskStatus;

namespace Manager.BusinessLogic.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<ProjectTask>> _tasksRepoMock;
    private readonly Mock<IRepository<Project>> _projectsRepoMock;
    private readonly Mock<IRepository<Employee>> _employeesRepoMock;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tasksRepoMock = new Mock<IRepository<ProjectTask>>();
        _projectsRepoMock = new Mock<IRepository<Project>>();
        _employeesRepoMock = new Mock<IRepository<Employee>>();

        _unitOfWorkMock.Setup(u => u.ProjectTasks).Returns(_tasksRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectsRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeesRepoMock.Object);

        _sut = new TaskService(_unitOfWorkMock.Object);
    }

    // CreateTaskAsync 

    [Fact]
    public async Task CreateTaskAsync_ProjectDoesNotExist_ThrowsNotFoundException()
    {
        _projectsRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new TaskCreateDto { Name = "Task", ProjectId = 999, Priority = 1 };

        var act = async () => await _sut.CreateTaskAsync(dto, authorId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTaskAsync_ExecutorDoesNotExist_ThrowsNotFoundException()
    {
        _projectsRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = new TaskCreateDto { Name = "Task", ProjectId = 1, ExecutorId = 999, Priority = 1 };

        var act = async () => await _sut.CreateTaskAsync(dto, authorId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateTaskAsync_NoExecutorSpecified_DoesNotCheckEmployeeExistence()
    {
        _projectsRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new TaskCreateDto { Name = "Task", ProjectId = 1, ExecutorId = null, Priority = 1 };

        await _sut.CreateTaskAsync(dto, authorId: 1);

        _employeesRepoMock.Verify(
            r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTaskAsync_Valid_CreatesTaskWithToDoStatusAndCorrectAuthor()
    {
        _projectsRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ProjectTask? captured = null;
        _tasksRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectTask, CancellationToken>((t, ct) => captured = t)
            .Returns(Task.CompletedTask);

        var dto = new TaskCreateDto { Name = "New Task", ProjectId = 1, Priority = 2 };

        await _sut.CreateTaskAsync(dto, authorId: 7);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(TaskStatus.ToDo);
        captured.AuthorId.Should().Be(7);
        captured.Name.Should().Be("New Task");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AssignTaskExecutorAsync 

    [Fact]
    public async Task AssignTaskExecutorAsync_TaskNotFound_ThrowsArgumentException()
    {
        _tasksRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProjectTask?)null);

        var act = async () => await _sut.AssignTaskExecutorAsync(999, executorId: 1, currentEmployeeId: 1, isAdmin: false);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AssignTaskExecutorAsync_NotManagerAndNotAdmin_ThrowsForbidden()
    {
        var task = new ProjectTask { Id = 1, ProjectId = 1 };
        var project = new Project { Id = 1, ManagerId = 5 };

        _tasksRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectsRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = async () => await _sut.AssignTaskExecutorAsync(1, executorId: 10, currentEmployeeId: 999, isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AssignTaskExecutorAsync_IsAdmin_AllowsAssignmentEvenIfNotManager()
    {
        var task = new ProjectTask { Id = 1, ProjectId = 1 };
        var project = new Project { Id = 1, ManagerId = 5 };

        _tasksRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectsRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.AssignTaskExecutorAsync(1, executorId: 10, currentEmployeeId: 999, isAdmin: true);

        task.ExecutorId.Should().Be(10);
    }

    [Fact]
    public async Task AssignTaskExecutorAsync_ExecutorDoesNotExist_ThrowsNotFoundException()
    {
        var task = new ProjectTask { Id = 1, ProjectId = 1 };
        var project = new Project { Id = 1, ManagerId = 5 };

        _tasksRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectsRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _employeesRepoMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.AssignTaskExecutorAsync(1, executorId: 999, currentEmployeeId: 5, isAdmin: false);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignTaskExecutorAsync_NullExecutorId_UnassignsWithoutCheckingExistence()
    {
        var task = new ProjectTask { Id = 1, ProjectId = 1, ExecutorId = 5 };
        var project = new Project { Id = 1, ManagerId = 42 };

        _tasksRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectsRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _sut.AssignTaskExecutorAsync(1, executorId: null, currentEmployeeId: 42, isAdmin: false);

        task.ExecutorId.Should().BeNull();
        _employeesRepoMock.Verify(
            r => r.AnyAsync(It.IsAny<Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // UpdateTaskStatusAsync

    [Fact]
    public async Task UpdateTaskStatusAsync_TaskNotFound_ThrowsArgumentException()
    {
        _tasksRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProjectTask?)null);

        var act = async () => await _sut.UpdateTaskStatusAsync(999, TaskStatus.Done, currentEmployeeId: 1, isAdmin: false);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(true, false, false, false, true)]   
    [InlineData(false, true, false, false, true)]   
    [InlineData(false, false, true, false, true)]   
    [InlineData(false, false, false, true, true)]   
    [InlineData(false, false, false, false, false)] 
    public async Task UpdateTaskStatusAsync_PermissionMatrix(bool isAdmin, bool isAuthor, bool isExecutor, bool isManager, bool shouldSucceed)
    {
        const int currentEmployeeId = 100;
        var task = new ProjectTask
        {
            Id = 1,
            ProjectId = 1,
            AuthorId = isAuthor ? currentEmployeeId : 1,
            ExecutorId = isExecutor ? currentEmployeeId : 2
        };
        var project = new Project { Id = 1, ManagerId = isManager ? currentEmployeeId : 3 };

        _tasksRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectsRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var act = async () => await _sut.UpdateTaskStatusAsync(1, TaskStatus.InProgress, currentEmployeeId, isAdmin);

        if (shouldSucceed)
        {
            await act.Should().NotThrowAsync();
            task.Status.Should().Be(TaskStatus.InProgress);
        }
        else
        {
            await act.Should().ThrowAsync<ForbiddenException>();
        }
    }
}