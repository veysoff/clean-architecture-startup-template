using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.Get;

internal sealed class GetTodosQueryHandler(IApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetTodosQuery, List<TodoResponse>>
{
    public async Task<Result<List<TodoResponse>>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
    {
        if (query.UserId != userContext.UserId)
        {
            return Result.Failure<List<TodoResponse>>(UserErrors.Unauthorized());
        }

        var todos = await context.TodoItems
            .Where(todoItem => todoItem.UserId == query.UserId)
            .Select(todoItem => new TodoResponse(
                todoItem.Id,
                todoItem.UserId,
                todoItem.Description,
                todoItem.DueDate,
                todoItem.Labels,
                todoItem.IsCompleted,
                todoItem.CreatedAt,
                todoItem.CompletedAt))
            .ToListAsync(cancellationToken);

        return todos;
    }
}
