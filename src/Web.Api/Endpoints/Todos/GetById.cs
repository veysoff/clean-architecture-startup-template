using Application.Abstractions.Messaging;
using Application.Todos.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Todos;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        app.MapGet("v{version:apiVersion}/todos/{id:guid}", async (
            Guid id,
            IQueryHandler<GetTodoByIdQuery, TodoResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetTodoByIdQuery(id);

            Result<TodoResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0))
        .WithTags(Tags.Todos)
        .RequireAuthorization()
        .RequireRateLimiting("authenticated");
    }
}
