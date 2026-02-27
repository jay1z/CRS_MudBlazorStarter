using MudBlazor;
//https://themes.arctechonline.tech/
namespace Horizon.Themes {
    public interface IMudTheme {
        string? Name { get; }
        MudTheme? Theme { get; }
    }
}
