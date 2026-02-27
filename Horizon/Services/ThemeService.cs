using System.Reflection;
using System.Text.Json;
using MudBlazor;
using Horizon.Services.Tenant;
using MudBlazor.Utilities;

public class ThemeService {
    public MudTheme CurrentTheme { get; private set; } = new MudTheme();
    public bool IsDarkMode { get; private set; }
    public event Action? OnThemeChanged;

    private readonly ITenantContext? _tenantContext;
    private readonly List<(string Name, MudTheme Theme)> _presets;
    private string? _lastSignature; // cache: "tenantId|hash(branding)"

    public ThemeService() {
        _presets = LoadPresets();
    }
    public ThemeService(ITenantContext tenantContext) {
        _tenantContext = tenantContext;
        _presets = LoadPresets();
    }

    public void SetTheme(MudTheme theme, bool? isDark = null) {
        CurrentTheme = theme;
        if (isDark.HasValue) IsDarkMode = isDark.Value;
        OnThemeChanged?.Invoke();
    }

    public void ToggleDarkMode(bool isDark) {
        IsDarkMode = isDark;
        OnThemeChanged?.Invoke();
    }

    // Apply a theme from BrandingJson stored on the tenant over a base theme (Default preset if available)
    public void ApplyTenantBrandingIfAvailable() {
        if (_tenantContext == null) return;
        if (string.IsNullOrWhiteSpace(_tenantContext.BrandingJson)) return;
        if (!TryParseBranding(_tenantContext.BrandingJson!, out var payload, out _)) return;
        var baseTheme = GetBasePreset(payload);
        var merged = MergeBranding(baseTheme, payload);
        SetTheme(merged, payload.UseDarkMode);
    }

    // Same as above but no-ops if signature unchanged for the request lifetime
    public void ApplyTenantBrandingIfAvailableAndChanged() {
        if (_tenantContext == null) return;
        var signature = $"{_tenantContext.TenantId}|{(_tenantContext.BrandingJson ?? string.Empty).GetHashCode()}";
        if (signature == _lastSignature) return;
        _lastSignature = signature;
        ApplyTenantBrandingIfAvailable();
    }

    // Allow external callers to request a UI refresh when theme/tenant context changed
    public void NotifyThemeChanged() => OnThemeChanged?.Invoke();

    // Presets
    public IReadOnlyList<string> GetPresetNames() => _presets.Select(p => p.Name).ToList();
    public MudTheme? GetPresetByName(string name) => _presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Theme;

    public string ToBrandingJson(MudTheme theme, bool? useDarkMode = null, string? preset = null) {
        var payload = new BrandingPayload(
        theme.PaletteLight?.Primary?.ToString(),
        theme.PaletteLight?.Secondary?.ToString(),
        theme.PaletteLight?.AppbarBackground?.ToString(),
        theme.PaletteLight?.Background?.ToString(),
        theme.PaletteDark?.Primary?.ToString(),
        theme.PaletteDark?.Secondary?.ToString(),
        null, // tertiary
        theme.PaletteLight?.Info?.ToString(),
        theme.PaletteLight?.Success?.ToString(),
        theme.PaletteLight?.Warning?.ToString(),
        theme.PaletteLight?.Error?.ToString(),
        null, null, useDarkMode, preset
        );
        return JsonSerializer.Serialize(payload);
    }

    public bool TryParseBranding(string json, out BrandingPayload payload, out string? error) {
        error = null;
        try {
            payload = JsonSerializer.Deserialize<BrandingPayload>(json) ?? new BrandingPayload();
            return true;
        } catch (Exception ex) {
            payload = new BrandingPayload();
            error = ex.Message;
            return false;
        }
    }

    public MudTheme MergeBranding(MudTheme baseTheme, BrandingPayload p) {
        var theme = new MudTheme {
            PaletteLight = new PaletteLight {
                Primary = ColorOr(baseTheme.PaletteLight?.Primary, p.Primary),
                Secondary = ColorOr(baseTheme.PaletteLight?.Secondary, p.Secondary),
                Tertiary = ColorOr(baseTheme.PaletteLight?.Tertiary, p.Tertiary),
                AppbarBackground = ColorOr(baseTheme.PaletteLight?.AppbarBackground, p.AppbarBackground),
                Background = ColorOr(baseTheme.PaletteLight?.Background, p.Background),
                Info = ColorOr(baseTheme.PaletteLight?.Info, p.Info),
                Success = ColorOr(baseTheme.PaletteLight?.Success, p.Success),
                Warning = ColorOr(baseTheme.PaletteLight?.Warning, p.Warning),
                Error = ColorOr(baseTheme.PaletteLight?.Error, p.Error)
            },
            PaletteDark = new PaletteDark {
                Primary = ColorOr(baseTheme.PaletteDark?.Primary, p.DarkPrimary),
                Secondary = ColorOr(baseTheme.PaletteDark?.Secondary, p.DarkSecondary)
            }
        };
        return theme;
    }

    private static MudColor? ColorOr(MudColor? fallback, string? hex)
    => string.IsNullOrWhiteSpace(hex) ? fallback : new MudColor(hex);

    private static MudTheme GetBasePreset(BrandingPayload p) {
        // Prefer requested preset if provided
        if (!string.IsNullOrWhiteSpace(p.Preset)) {
            var svc = new ThemeService();
            var preset = svc.GetPresetByName(p.Preset!);
            if (preset != null) return preset;
        }
        return new MudTheme();
    }

    private static List<(string Name, MudTheme Theme)> LoadPresets() {
        var list = new List<(string, MudTheme)>();
        try {
            var asm = typeof(Horizon.Themes.IMudTheme).Assembly;
            var types = asm.GetTypes().Where(t => !t.IsAbstract && typeof(Horizon.Themes.IMudTheme).IsAssignableFrom(t));
            foreach (var t in types) {
                var instance = Activator.CreateInstance(t) as Horizon.Themes.IMudTheme;
                if (instance?.Theme != null) list.Add((instance.Name ?? t.Name, instance.Theme));
            }
        } catch {
            // ignore reflection issues
        }
        return list;
    }

    public record BrandingPayload(
    string? Primary = null,
    string? Secondary = null,
    string? AppbarBackground = null,
    string? Background = null,
    string? DarkPrimary = null,
    string? DarkSecondary = null,
    string? Tertiary = null,
    string? Info = null,
    string? Success = null,
    string? Warning = null,
    string? Error = null,
    string? TextPrimary = null,
    string? TextSecondary = null,
    bool? UseDarkMode = null,
    string? Preset = null,
    // Organization Contact Info (shared across system)
    string? CompanyTagline = null,
    string? CompanyPhone = null,
    string? CompanyEmail = null,
    string? CompanyWebsite = null,
    string? CompanyAddress = null,
    string? CompanyLogoUrl = null,
    // PDF-specific settings
    Horizon.Models.PdfSettings? PdfSettings = null
    );
}
