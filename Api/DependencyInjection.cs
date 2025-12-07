using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Api.Infrastructure.Exceptions;
using Api.Services;
using Application.Common.Interfaces.Services;
using Application.Configuration;
using Domain.Entities.Identity;
using Duende.IdentityServer.Services;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Constants = Domain.Static.Constants;

namespace Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddWebServices(
        this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment,
        ConfigureHostBuilder host)
    {
        configuration
            .AddConfigurationFiles();

        services
            .BindSystemConfigurations()
            .ConfigureLogging(configuration, host)
            .ConfigureSwagger()
            .ConfigureHealthChecks()
            .ConfigureExceptionHandlers()
            .ConfigureAuthentication(configuration, environment)
            .ConfigureApiFeatures()
            .RegisterApiServices();

        return services;
    }

    private static IServiceCollection ConfigureHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<DbContext>();
        return services;
    }

    private static IServiceCollection BindSystemConfigurations(this IServiceCollection services)
    {
        services
            .AddOptions<CorsConfiguration>()
            .BindConfiguration(CorsConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        //services
        //    .AddOptions<EmailConfiguration>()
        //    .BindConfiguration(EmailConfiguration.SectionName)
        //    .ValidateOnStart()
        //    .ValidateDataAnnotations();

        services
            .AddOptions<IdentityApiConfiguration>()
            .BindConfiguration(IdentityApiConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
            .AddOptions<SwaggerConfiguration>()
            .BindConfiguration(SwaggerConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
            .AddOptions<ReCaptchaConfiguration>()
            .BindConfiguration(ReCaptchaConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
           .AddOptions<ODataConfiguration>()
           .BindConfiguration(ODataConfiguration.SectionName)
           .ValidateOnStart()
           .ValidateDataAnnotations();

        services
           .AddOptions<UrlsConfiguration>()
           .BindConfiguration(UrlsConfiguration.SectionName)
           .ValidateOnStart()
           .ValidateDataAnnotations();

        return services;
    }

    private static IServiceCollection ConfigureLogging(this IServiceCollection services, IConfiguration configuration, ConfigureHostBuilder host)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom
            .Configuration(configuration)
            .CreateLogger();

        services.AddSerilog();
        host.UseSerilog();

        return services;
    }

    private static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        using ServiceProvider scope = services.BuildServiceProvider();
        SwaggerConfiguration swaggerConfig = scope.GetRequiredService<IOptions<SwaggerConfiguration>>().Value;

        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo
            {
                Title = swaggerConfig.Title,
                Description = swaggerConfig.Description,
                Contact = new OpenApiContact
                {
                    Name = swaggerConfig.Contact.Name
                },
            });
        });

        return services;
    }

    private static IServiceCollection ConfigureAuthentication(this IServiceCollection services, ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        using ServiceProvider scope = services.BuildServiceProvider();
        IdentityApiConfiguration identityConfig = scope.GetRequiredService<IOptions<IdentityApiConfiguration>>().Value;
        UrlsConfiguration urlsConfig = scope.GetRequiredService<IOptions<UrlsConfiguration>>().Value;

        var authority = new Uri(urlsConfig.Authority);

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;

                options.Stores.MaxLengthForKeys = 128;

                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddIdentityServer(opt =>
            {
                opt.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;

                opt.KeyManagement.Enabled = false;

                // Events
                opt.Events.RaiseErrorEvents = true;
                opt.Events.RaiseInformationEvents = true;
                opt.Events.RaiseFailureEvents = true;
                opt.Events.RaiseSuccessEvents = true;
            })
            .AddSigningCredential(GetSigningCredentials())
            .AddPersistedGrantStore<UserGrantStore>()
            .AddInMemoryClients(identityConfig.Clients)
            .AddInMemoryIdentityResources(IdentityServerStore.GetIdentityResources())
            .AddInMemoryApiResources(IdentityServerStore.GetApiResources())
            .AddInMemoryApiScopes(IdentityServerStore.GetApiScopes())
            .AddAspNetIdentity<ApplicationUser>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = IdentityConstants.ApplicationScheme + "-" + authority.Port.ToString();
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromSeconds(configuration.GetSection("CookiesConfiguration:ApplicationCookieTimeoutInSeconds").Get<int>());
            options.SlidingExpiration = configuration.GetSection("CookiesConfiguration:SlidingExpiration").Get<bool>();
            options.Cookie.IsEssential = true;
            options.Cookie.MaxAge = options.ExpireTimeSpan;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
        });

        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.SameSite = environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        services.AddTransient<IProfileService, CustomProfileService>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "AuthenticationHandlerScheme";
            })
            .AddPolicyScheme("AuthenticationHandlerScheme", "Selector between Cookie and JWT", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // Check for JWT Bearer token
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }

                    // Check for our application cookie
                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    LifetimeValidator = (_, expires, _, _) => expires >= DateTime.UtcNow,
                };

                options.RequireHttpsMetadata = false;
                options.Authority = urlsConfig.Authority;
            });

        services.AddAuthorization();
        services.AddHttpClient();

        return services;
    }

    private static IServiceCollection ConfigureApiFeatures(this IServiceCollection services)
    {
        using ServiceProvider scope = services.BuildServiceProvider();
        ODataConfiguration odataConfig = scope.GetRequiredService<IOptions<ODataConfiguration>>().Value;

        services.AddLocalization(options => options.ResourcesPath = Constants.Localization.DirectoryPath);
        services.Configure<RequestLocalizationOptions>(options =>
        {
            CultureInfo[] supportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            options.DefaultRequestCulture = new RequestCulture(Constants.Localization.DefaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        services.Configure<ApiBehaviorOptions>(config =>
        {
            config.SuppressModelStateInvalidFilter = true;
        });

        services.AddControllers().AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .AddOData(options =>
            options.Select()
                   .Filter()
                   .OrderBy()
                   .Expand()
                   .Count()
                   .SetMaxTop(odataConfig.SelectTop));

        services.AddSpaStaticFiles(configuration => configuration.RootPath = Constants.ClientApp.DistPath);

        return services;
    }

    private static IServiceCollection ConfigureExceptionHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    private static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    private static ConfigurationManager AddConfigurationFiles(this ConfigurationManager configuration)
    {
        configuration.AddJsonFile(Constants.Configuration.Files.Serilog, true, true);

        return configuration;
    }

    private static SigningCredentials GetSigningCredentials()
    {
        RSA rsaPrivateKey = RSA.Create();
        rsaPrivateKey.ImportPkcs8PrivateKey(Convert.FromBase64String(Constants.IdentityApi.SigningCredentials.PrivateKey), out _);

        RsaSecurityKey rsaSecurityKey = new(rsaPrivateKey);

        return new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);
    }
}
