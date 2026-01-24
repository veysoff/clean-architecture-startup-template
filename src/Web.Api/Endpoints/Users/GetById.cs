using Application.Abstractions.Messaging;
using Application.Users.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        app.MapGet("v{version:apiVersion}/users/{userId}", async (
            Guid userId,
            IQueryHandler<GetUserByIdQuery, UserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserByIdQuery(userId);

            Result<UserResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new Asp.Versioning.ApiVersion(1, 0))
        .HasPermission(Permissions.UsersAccess)
        .WithTags(Tags.Users)
        .RequireRateLimiting("authenticated");
    }
}
