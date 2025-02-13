using MudBlazor;

namespace CRS.Themes {
    public interface IMudTheme {
        string? Name { get; }
        MudTheme? Theme { get; }
    }
}
