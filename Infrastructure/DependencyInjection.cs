using Infrastructure.Interceptors;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        string? connectionString = configuration.GetConnectionString(Constants.Database.DefaultConnectionString);

        ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));

        services
            .AddDbContext(connectionString)
            .AddHybridCache(configuration)
            .RegisterInfrastructureServices();

        return services;
    }

    public static IServiceCollection AddHybridCache(this IServiceCollection services, ConfigurationManager configuration)
    {
        int expiration = configuration.GetValue<int>(Constants.ConfigurationKeys.DefaultExpirationInSeconds);

        services.AddHybridCache(OptionsBuilderConfigurationExtensions =>
        {
            OptionsBuilderConfigurationExtensions.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(expiration),
                LocalCacheExpiration = TimeSpan.FromSeconds(expiration),
            };
        });

        return services;
    }

    private static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IReCaptchaService, ReCaptchaService>();

        return services;
    }

    private static IServiceCollection AddDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>(
            config =>
            {
                config.UseSqlServer(connectionString);
            });

        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
