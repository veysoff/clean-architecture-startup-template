using Application.Users.Register;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users.Register;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldHaveError_WhenFirstNameIsEmpty()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            FirstName: "",
            LastName: "User",
            Password: "password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenLastNameIsEmpty()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "",
            Password: "password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "",
            FirstName: "Test",
            LastName: "User",
            Password: "password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "invalid-email",
            FirstName: "Test",
            LastName: "User",
            Password: "password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@no-local-part.com")]
    [InlineData("missing-at-sign.com")]
    public void Validate_ShouldHaveError_WhenEmailFormatIsInvalid(string invalidEmail)
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: invalidEmail,
            FirstName: "Test",
            LastName: "User",
            Password: "password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooShort()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "pass"); // Only 4 characters, minimum is 8

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenPasswordIs8Characters()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "12345678"); // Exactly 8 characters

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "SecurePassword123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("user+tag@example.com")]
    public void Validate_ShouldNotHaveError_WhenEmailIsValid(string validEmail)
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: validEmail,
            FirstName: "Test",
            LastName: "User",
            Password: "password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}
