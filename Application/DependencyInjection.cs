using System.Reflection;
using Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        _ = services
            .AddFluentValidation()
            .AddAutoMapper()
            .AddMediatR();

        return services;
    }

    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        _ = services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        _ = services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddMediatR(this IServiceCollection services)
    {
        _ = services.AddMediatR(config =>
        {
            _ = config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            _ = config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
