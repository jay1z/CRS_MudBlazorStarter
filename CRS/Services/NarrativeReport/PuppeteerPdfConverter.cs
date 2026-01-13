using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// PDF converter implementation using PuppeteerSharp (headless Chrome).
/// Provides high-fidelity HTML to PDF conversion with full CSS support.
/// </summary>
public class PuppeteerPdfConverter : IPdfConverter, IAsyncDisposable
{
    private readonly ILogger<PuppeteerPdfConverter> _logger;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private IBrowser? _browser;
    private bool _browserDownloaded;
    private bool _disposed;

    public PuppeteerPdfConverter(ILogger<PuppeteerPdfConverter> logger)
    {
        _logger = logger;
    }

    public string EngineName => "PuppeteerSharp (Chromium)";

    /// <inheritdoc />
    public async Task<byte[]> ConvertToPdfAsync(
        string html,
        PdfConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PdfConversionOptions();

        var browser = await GetOrCreateBrowserAsync(cancellationToken);
        await using var page = await browser.NewPageAsync();

        try
        {
            // Set content with timeout
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0],
                Timeout = options.TimeoutSeconds * 1000
            });

            // Wait for any web fonts or images to load
            await page.EvaluateExpressionAsync("document.fonts.ready");

            // Configure PDF options
            var pdfOptions = new PdfOptions
            {
                Format = MapPageSize(options.PageSize),
                Landscape = options.Orientation == PdfOrientation.Landscape,
                PrintBackground = options.PrintBackground,
                MarginOptions = new MarginOptions
                {
                    Top = $"{options.MarginTop}in",
                    Bottom = $"{options.MarginBottom}in",
                    Left = $"{options.MarginLeft}in",
                    Right = $"{options.MarginRight}in"
                },
                DisplayHeaderFooter = !string.IsNullOrEmpty(options.HeaderHtml) || 
                                      !string.IsNullOrEmpty(options.FooterHtml) ||
                                      options.ShowPageNumbers
            };

            // Configure header/footer
            if (pdfOptions.DisplayHeaderFooter)
            {
                pdfOptions.HeaderTemplate = options.HeaderHtml ?? "<span></span>";
                pdfOptions.FooterTemplate = BuildFooterTemplate(options);
            }

            var pdfBytes = await page.PdfDataAsync(pdfOptions);

            _logger.LogDebug("Generated PDF: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert HTML to PDF");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBrowserDownloadedAsync();
            var browser = await GetOrCreateBrowserAsync(cancellationToken);
            return browser != null && !browser.IsClosed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF converter is not available");
            return false;
        }
    }

    private async Task<IBrowser> GetOrCreateBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser != null && !_browser.IsClosed)
            return _browser;

        await _browserLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser != null && !_browser.IsClosed)
                return _browser;

            await EnsureBrowserDownloadedAsync();

            _logger.LogInformation("Launching Chromium browser for PDF generation");
            
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--disable-extensions",
                    "--disable-background-networking",
                    "--disable-sync",
                    "--disable-translate",
                    "--hide-scrollbars",
                    "--mute-audio",
                    "--no-first-run",
                    "--safebrowsing-disable-auto-update"
                }
            });

            _logger.LogInformation("Chromium browser launched successfully");
            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private async Task EnsureBrowserDownloadedAsync()
    {
        if (_browserDownloaded) return;

        await _browserLock.WaitAsync();
        try
        {
            if (_browserDownloaded) return;

            _logger.LogInformation("Downloading Chromium browser...");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            _browserDownloaded = true;
            _logger.LogInformation("Chromium browser downloaded successfully");
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private static PaperFormat MapPageSize(string pageSize)
    {
        return pageSize.ToUpperInvariant() switch
        {
            "LETTER" => PaperFormat.Letter,
            "LEGAL" => PaperFormat.Legal,
            "TABLOID" => PaperFormat.Tabloid,
            "LEDGER" => PaperFormat.Ledger,
            "A0" => PaperFormat.A0,
            "A1" => PaperFormat.A1,
            "A2" => PaperFormat.A2,
            "A3" => PaperFormat.A3,
            "A4" => PaperFormat.A4,
            "A5" => PaperFormat.A5,
            "A6" => PaperFormat.A6,
            _ => PaperFormat.Letter
        };
    }

    private static string BuildFooterTemplate(PdfConversionOptions options)
    {
        if (!string.IsNullOrEmpty(options.FooterHtml))
            return options.FooterHtml;

        if (options.ShowPageNumbers)
        {
            return """
                <div style="font-size: 9px; width: 100%; text-align: center; color: #666;">
                    <span class="pageNumber"></span> of <span class="totalPages"></span>
                </div>
                """;
        }

        return "<span></span>";
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser != null)
        {
            try
            {
                await _browser.CloseAsync();
                _browser.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing browser");
            }
        }

        _browserLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
