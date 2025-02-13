using MudBlazor;

public class ThemeService {
    public MudTheme CurrentTheme { get; private set; } = new MudTheme();

    public event Action? OnThemeChanged;

    public void SetTheme(MudTheme theme) {
        CurrentTheme = theme;
        //OnThemeChanged?.Invoke();
    }
}
