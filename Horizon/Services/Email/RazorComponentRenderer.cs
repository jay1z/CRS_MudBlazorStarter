using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

using System.Text;

namespace Horizon.Services.Email {
    public interface IRazorComponentRenderer {
        Task<string> RenderComponentAsync<TComponent>(Dictionary<string, object> parameters)
            where TComponent : IComponent;

        Task<string> RenderComponentAsync<TComponent>(Dictionary<string, object> parameters, bool enableCaching, TimeSpan? cacheDuration = null)
            where TComponent : IComponent;
    }


    public class RazorComponentRenderer : IRazorComponentRenderer {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RazorComponentRenderer> _logger;
        private readonly HtmlRenderer _htmlRenderer;
        private readonly IMemoryCache _cache;

        public RazorComponentRenderer(
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IMemoryCache cache) {
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<RazorComponentRenderer>();
            _htmlRenderer = new HtmlRenderer(_serviceProvider, loggerFactory);
            _cache = cache;
        }

        public async Task<string> RenderComponentAsync<TComponent>(Dictionary<string, object> parameters)
            where TComponent : IComponent {
            return await RenderComponentAsync<TComponent>(parameters, enableCaching: false);
        }

        public async Task<string> RenderComponentAsync<TComponent>(
            Dictionary<string, object> parameters,
            bool enableCaching,
            TimeSpan? cacheDuration = null)
            where TComponent : IComponent {
            try {
                if (!enableCaching) {
                    return await RenderComponentDirectly<TComponent>(parameters);
                }

                // Create a cache key based on component type and parameters
                var cacheKey = CreateCacheKey<TComponent>(parameters);

                // Try to get from cache first
                if (_cache.TryGetValue(cacheKey, out string cachedResult)) {
                    _logger.LogDebug("Retrieved cached rendering for {ComponentName}", typeof(TComponent).Name);
                    return cachedResult;
                }

                // Render the component
                var result = await RenderComponentDirectly<TComponent>(parameters);

                // Cache the result
                var duration = cacheDuration ?? TimeSpan.FromMinutes(10);
                _cache.Set(cacheKey, result, duration);

                return result;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error rendering component {ComponentName}", typeof(TComponent).Name);
                throw;
            }
        }

        private Task<string> RenderComponentDirectly<TComponent>(Dictionary<string, object> parameters)
            where TComponent : IComponent {
            var parameterView = ParameterView.FromDictionary(parameters);

            return _htmlRenderer.Dispatcher.InvokeAsync(async () => {
                // Wrap the component inside ProviderWrapper to ensure MudBlazor overlay providers are present for server-side HTML rendering
                var wrapperParams = new Dictionary<string, object> {
                    { "ChildContent", (RenderFragment)(builder => {
                        builder.OpenComponent(0, typeof(TComponent));
                        foreach (var p in parameters) builder.AddAttribute(1, p.Key, p.Value);
                        builder.CloseComponent();
                    }) }
                };

                var output = await _htmlRenderer.RenderComponentAsync<Horizon.Components.Shared.ProviderWrapper>(ParameterView.FromDictionary(wrapperParams));
                return output.ToHtmlString();
            });
        }

        private string CreateCacheKey<TComponent>(Dictionary<string, object> parameters)
            where TComponent : IComponent {
            var componentName = typeof(TComponent).FullName;
            var paramHash = string.Join("_", parameters.OrderBy(p => p.Key)
                .Select(p => $"{p.Key}={p.Value?.GetHashCode() ?? 0}"));

            return $"ComponentRenderer_{componentName}_{paramHash}";
        }
    }

}
