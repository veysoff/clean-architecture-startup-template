using Application.Todos.Create;
using Domain.Todos;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Todos.Create;

public sealed class CreateTodoCommandValidatorTests
{
    private readonly CreateTodoCommandValidator _validator;

    public CreateTodoCommandValidatorTests()
    {
        _validator = new CreateTodoCommandValidator();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.Empty,
            Description = "Test todo",
            Priority = Priority.High
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDescriptionIsEmpty()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "",
            Priority = Priority.High
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = new string('a', 256), // 256 characters, max is 255
            Priority = Priority.High
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPriorityIsInvalid()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Test todo",
            Priority = (Priority)999 // Invalid enum value
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDueDateIsInPast()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Test todo",
            Priority = Priority.High,
            DueDate = DateTime.Today.AddDays(-1)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenDueDateIsToday()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Test todo",
            Priority = Priority.High,
            DueDate = DateTime.Today
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenDueDateIsInFuture()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Test todo",
            Priority = Priority.High,
            DueDate = DateTime.Today.AddDays(7)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenDueDateIsNull()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Test todo",
            Priority = Priority.High,
            DueDate = null
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Valid todo description",
            Priority = Priority.Medium,
            DueDate = DateTime.Today.AddDays(3),
            Labels = ["label1", "label2"]
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
