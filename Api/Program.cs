using Api;
using Api.Infrastructure.Extensions;
using Application;
using Infrastructure;
using Infrastructure.Extensions;
using Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        /*
         * Add services to the container.
         */

        builder.Services.AddCors();

        AddDataProtection(builder);

        var openAIConf = builder.Configuration.GetSection("OpenAIConfiguration");

        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddApplicationServices();

        builder.Services.AddWebServices(builder.Configuration, builder.Environment, builder.Host);

        WebApplication app = builder.Build();

        /*
         *
         * Configure the HTTP request pipeline.
         *
         */
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(s => s.SwaggerEndpoint(
            Constants.Swagger.Route,
            Constants.Swagger.Title));
        }
        else
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();

        await app.InitializeDatabaseAsync();

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseSpaStaticFiles();

        app.UseSerilogRequestLogging();

        app.UseRequestLocalization();

        app.UseCors();

        app.UseRouting();

        app.UseAuthentication();

        app.UseIdentityServer();

        app.UseAuthorization();

        app.UseSwagger();

        app.UseHealthChecks(Constants.HealthCheck.Route);

        app.MapControllers();

        app.UseSpa((options) => options.Options.SourcePath = Constants.ClientApp.RootPath);

        await app.RunAsync();
    }

    private static void AddDataProtection(WebApplicationBuilder builder)
    {
        var urlsConfig = builder.Configuration.GetSection("UrlsConfiguration");

        var uri = new Uri(urlsConfig.GetValue<string>("Authority") ?? string.Empty);
        var uniqueDeployName = "Clean_Application_Template" + uri.Port;

        builder.Services.AddDataProtection()
                        .PersistKeysToDbContext<ApplicationDbContext>()
                        .SetApplicationName(uniqueDeployName);
    }
}