namespace Application.Users.GetByEmail;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName);
