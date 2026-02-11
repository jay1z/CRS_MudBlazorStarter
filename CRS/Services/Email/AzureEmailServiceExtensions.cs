using Coravel;
using Coravel.Mailer.Mail.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace CRS.Services.Email;

/// <summary>
/// Extension methods for registering Azure Communication Services email.
/// </summary>
public static class AzureEmailServiceExtensions
{
    /// <summary>
    /// Adds Azure Communication Services email as the IMailer implementation.
    /// Call this INSTEAD of AddMailer() when using Azure email.
    /// </summary>
    public static IServiceCollection AddAzureEmailMailer(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<AzureEmailOptions>(configuration.GetSection(AzureEmailOptions.SectionName));

        // Add MVC view services required for Razor email template rendering
        // This registers IRazorViewEngine, ITempDataProvider, etc.
        services.AddControllersWithViews();

        // Register Razor view renderer for email templates
        services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();

        // Register the Azure mailer as IMailer
        services.AddSingleton<IMailer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureEmailOptions>>();
            var logger = sp.GetRequiredService<ILogger<AzureCommunicationMailer>>();
            return new AzureCommunicationMailer(options, logger, sp);
        });

        return services;
    }

    /// <summary>
    /// Adds email services with automatic provider selection based on configuration.
    /// Uses Azure Communication Services if AzureEmail:Enabled is true, otherwise falls back to Coravel SMTP.
    /// </summary>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        var azureEmailEnabled = configuration.GetValue<bool>("AzureEmail:Enabled");

        if (azureEmailEnabled)
        {
            services.AddAzureEmailMailer(configuration);
        }
        else
        {
            // Fall back to Coravel SMTP mailer
            services.AddMailer(configuration);
        }

        return services;
    }
}

/// <summary>
/// Renders Razor views to string for email templates.
/// </summary>
public class RazorViewToStringRenderer : IRazorViewToStringRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public RazorViewToStringRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        using var sw = new StringWriter();

        // Try to find the view
        var viewResult = _viewEngine.FindView(actionContext, viewName, false);

        if (!viewResult.Success)
        {
            // Try with full path
            viewResult = _viewEngine.GetView(null, viewName, false);
        }

        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Could not find view '{viewName}'. Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? Array.Empty<string>())}");
        }

        var viewDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            sw,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
}
