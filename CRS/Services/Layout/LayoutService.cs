using System;

namespace Horizon.Services.Layout
{
    public class LayoutService : ILayoutService
    {
        private bool _hideNav;
        public bool HideNav => _hideNav;

        public event Action? OnChange;

        public void SetHideNav(bool hide)
        {
            if (_hideNav == hide) return;
            _hideNav = hide;
            OnChange?.Invoke();
        }
    }
}
