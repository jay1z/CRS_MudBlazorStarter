using System;

namespace CRS.Components.Layout
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class HideNavAttribute : Attribute
    {
    }
}
