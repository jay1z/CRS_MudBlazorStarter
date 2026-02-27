namespace Horizon.Services.Layout
{
    public interface ILayoutService
    {
        bool HideNav { get; }
        event Action? OnChange;
        void SetHideNav(bool hide);
    }
}
