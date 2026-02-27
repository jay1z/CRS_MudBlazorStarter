using MudBlazor;

namespace Horizon.Themes {
    public class Default : IMudTheme {
        public string? Name { get; } = "Default";

        public MudTheme? Theme => new MudTheme();
    }
}
