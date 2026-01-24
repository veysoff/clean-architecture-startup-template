using Application.Abstractions.Messaging;
using Application.Todos.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Todos;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        app.MapGet("v{version:apiVersion}/todos", async (
            Guid userId,
            IQueryHandler<GetTodosQuery, List<TodoResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTodosQuery(userId);

            Result<List<TodoResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0))
        .WithTags(Tags.Todos)
        .RequireAuthorization()
        .RequireRateLimiting("authenticated");
    }
}
