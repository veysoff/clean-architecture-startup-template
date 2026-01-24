using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Users.Login;
using Application.UnitTests.Infrastructure;
using Domain.Users;
using SharedKernel;

namespace Application.UnitTests.Users.Login;

public sealed class LoginUserCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenProviderMock = new Mock<ITokenProvider>();

        _handler = new LoginUserCommandHandler(
            _contextMock.Object,
            _passwordHasherMock.Object,
            _tokenProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFoundByEmail()
    {
        // Arrange
        var users = new List<User>();
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        var command = new LoginUserCommand("nonexistent@example.com", "password123");

        // Act
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.NotFoundByEmail");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed_password"
        };

        var users = new List<User> { user };
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        _passwordHasherMock.Setup(x => x.Verify("wrong_password", "hashed_password"))
            .Returns(false);

        var command = new LoginUserCommand("test@example.com", "wrong_password");

        // Act
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Users.NotFoundByEmail");
    }

    [Fact]
    public async Task Handle_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed_password"
        };

        var users = new List<User> { user };
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        _passwordHasherMock.Setup(x => x.Verify("correct_password", "hashed_password"))
            .Returns(true);

        const string expectedToken = "jwt_token_12345";
        _tokenProviderMock.Setup(x => x.Create(It.Is<User>(u => u.Id == user.Id)))
            .Returns(expectedToken);

        var command = new LoginUserCommand("test@example.com", "correct_password");

        // Act
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedToken);

        _passwordHasherMock.Verify(
            x => x.Verify("correct_password", "hashed_password"),
            Times.Once);

        _tokenProviderMock.Verify(
            x => x.Create(It.Is<User>(u => u.Id == user.Id)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCallTokenProvider_WhenUserNotFound()
    {
        // Arrange
        var users = new List<User>();
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        var command = new LoginUserCommand("nonexistent@example.com", "password123");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenProviderMock.Verify(
            x => x.Create(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCallTokenProvider_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed_password"
        };

        var users = new List<User> { user };
        var mockUserDbSet = MockDbSetFactory.CreateMockDbSet(users);
        _contextMock.Setup(x => x.Users).Returns(mockUserDbSet.Object);

        _passwordHasherMock.Setup(x => x.Verify("wrong_password", "hashed_password"))
            .Returns(false);

        var command = new LoginUserCommand("test@example.com", "wrong_password");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenProviderMock.Verify(
            x => x.Create(It.IsAny<User>()),
            Times.Never);
    }
}
