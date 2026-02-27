using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Horizon.Services.NarrativeReport;

/// <summary>
/// PDF converter implementation using PuppeteerSharp (headless Chrome).
/// Provides high-fidelity HTML to PDF conversion with full CSS support.
/// </summary>
public class PuppeteerPdfConverter : IPdfConverter, IAsyncDisposable
{
    private readonly ILogger<PuppeteerPdfConverter> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
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

        _logger.LogDebug("Starting PDF conversion, HTML length: {Length} chars", html.Length);

        try
        {
            var browser = await GetOrCreateBrowserAsync(cancellationToken);
            _logger.LogDebug("Browser acquired, creating new page");

            await using var page = await browser.NewPageAsync();
            _logger.LogDebug("Page created, setting content");

            // Set content with timeout
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Load], // Changed from Networkidle0 for faster processing
                Timeout = options.TimeoutSeconds * 1000
            });
            _logger.LogDebug("Content set, waiting for fonts");

            // Wait for any web fonts to load (with timeout)
            try
            {
                await page.EvaluateExpressionAsync("document.fonts.ready");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Font loading check failed, continuing anyway");
            }

            _logger.LogDebug("Generating PDF");

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

            _logger.LogInformation("Generated PDF: {Size} bytes", pdfBytes.Length);
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

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser != null && !_browser.IsClosed)
                return _browser;

            // Download browser if needed (inside lock to prevent concurrent downloads)
            if (!_browserDownloaded)
            {
                _logger.LogInformation("Downloading Chromium browser (first-time setup)...");
                var browserFetcher = new BrowserFetcher();
                var installedBrowser = await browserFetcher.DownloadAsync();
                _logger.LogInformation("Chromium downloaded to: {Path}", installedBrowser.GetExecutablePath());
                _browserDownloaded = true;
            }

            _logger.LogInformation("Launching Chromium browser for PDF generation");

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args =
                [
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
                ]
            });

            _logger.LogInformation("Chromium browser launched successfully (PID: {Pid})", _browser.Process?.Id);
            return _browser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Chromium browser");
            throw;
        }
        finally
        {
            _initLock.Release();
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

                    _initLock.Dispose();
                    GC.SuppressFinalize(this);
                }
            }
