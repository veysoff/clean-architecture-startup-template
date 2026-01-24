using Application.Abstractions.Caching;
using Application.Abstractions.Messaging;

namespace Application.Todos.GetById;

public sealed record GetTodoByIdQuery(Guid TodoItemId) : IQuery<TodoResponse>, ICachedQuery
{
    public string CacheKey => $"todo:{TodoItemId}";

    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
