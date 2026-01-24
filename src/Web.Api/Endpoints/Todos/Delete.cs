using Application.Abstractions.Messaging;
using Application.Todos.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Todos;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        app.MapDelete("v{version:apiVersion}/todos/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteTodoCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0))
        .WithTags(Tags.Todos)
        .RequireAuthorization()
        .RequireRateLimiting("authenticated");
    }
}
