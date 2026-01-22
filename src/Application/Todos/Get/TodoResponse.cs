namespace Application.Todos.Get;

public sealed record TodoResponse(
    Guid Id,
    Guid UserId,
    string Description,
    DateTime? DueDate,
    List<string> Labels,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt);
