using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Todos.Create;
using Application.UnitTests.Infrastructure;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Todos.Create;

public sealed class CreateTodoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly CreateTodoCommandHandler _handler;
    private readonly DateTime _fixedDateTime = new(2026, 1, 23, 12, 0, 0, DateTimeKind.Utc);

    public CreateTodoCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _userContextMock = new Mock<IUserContext>();

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(_fixedDateTime);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateTodoCommandHandler(
            _contextMock.Object,
            _dateTimeProviderMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        _userContextMock.Setup(x => x.UserId).Returns(differentUserId);

        var command = new CreateTodoCommand
        {
            UserId = userId,
            Description = "Test todo",
            Priority = Priority.High
        };

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.UserId).Returns(userId);

        var users = new List<User>();
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        var command = new CreateTodoCommand
        {
            UserId = userId,
            Description = "Test todo",
            Priority = Priority.High
        };

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldCreateTodo_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.UserId).Returns(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };

        var users = new List<User> { user };
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        var todosList = new List<TodoItem>();
        var mockTodoDbSet = MockDbSetFactory.CreateMockDbSet(todosList);
        _contextMock.Setup(x => x.TodoItems).Returns(mockTodoDbSet.Object);

        var command = new CreateTodoCommand
        {
            UserId = userId,
            Description = "Test todo",
            Priority = Priority.High,
            DueDate = _fixedDateTime.AddDays(7),
            Labels = ["important", "urgent"]
        };

        TodoItem? capturedTodo = null;
        mockTodoDbSet.Setup(x => x.Add(It.IsAny<TodoItem>()))
            .Callback<TodoItem>(todo => capturedTodo = todo);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        capturedTodo.Should().NotBeNull();
        capturedTodo!.UserId.Should().Be(userId);
        capturedTodo.Description.Should().Be("Test todo");
        capturedTodo.Priority.Should().Be(Priority.High);
        capturedTodo.DueDate.Should().Be(_fixedDateTime.AddDays(7));
        capturedTodo.Labels.Should().BeEquivalentTo("important", "urgent");
        capturedTodo.IsCompleted.Should().BeFalse();
        capturedTodo.CreatedAt.Should().Be(_fixedDateTime);

        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRaiseTodoCreatedDomainEvent_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.UserId).Returns(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };

        var users = new List<User> { user };
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        var todosList = new List<TodoItem>();
        var mockTodoDbSet = MockDbSetFactory.CreateMockDbSet(todosList);
        _contextMock.Setup(x => x.TodoItems).Returns(mockTodoDbSet.Object);

        var command = new CreateTodoCommand
        {
            UserId = userId,
            Description = "Test todo",
            Priority = Priority.High
        };

        TodoItem? capturedTodo = null;
        mockTodoDbSet.Setup(x => x.Add(It.IsAny<TodoItem>()))
            .Callback<TodoItem>(todo => capturedTodo = todo);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedTodo.Should().NotBeNull();
        capturedTodo!.DomainEvents.Should().HaveCount(1);
        capturedTodo.DomainEvents[0].Should().BeOfType<TodoItemCreatedDomainEvent>();
    }
}
