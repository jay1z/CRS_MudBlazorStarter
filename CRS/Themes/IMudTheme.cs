using MudBlazor;
//https://themes.arctechonline.tech/
namespace CRS.Themes {
    public interface IMudTheme {
        string? Name { get; }
        MudTheme? Theme { get; }
    }
}
