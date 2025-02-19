using MudBlazor;

namespace CRS.Themes {
    public class Default : IMudTheme {
        public string? Name { get; } = "Default";

        public MudTheme? Theme => new MudTheme();
    }
}
