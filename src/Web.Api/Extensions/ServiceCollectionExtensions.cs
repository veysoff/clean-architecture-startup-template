namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Clean Architecture API",
                Version = "v1",
                Description = "Clean Architecture .NET 10 API with CQRS + DDD + Rate Limiting + Caching",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Clean Architecture Team"
                }
            });
        });

        return services;
    }
}
