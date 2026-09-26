using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GitHelper
{
    /// <summary>
    /// The application icon, loaded once from the embedded .ico. Loading the
    /// icon file (rather than <c>Icon.ExtractAssociatedIcon</c>) keeps all nine
    /// sizes, so Windows can pick the right one for the title bar, Alt-Tab and
    /// the taskbar instead of scaling a 32px copy.
    /// </summary>
    public static class AppIcon
    {
        private const string ResourceName = "GitHelper.ico";

        private static Icon _icon;
        private static bool _tried;

        public static Icon Value
        {
            get
            {
                if (_tried) return _icon;
                _tried = true;

                try
                {
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                    {
                        if (stream != null) _icon = new Icon(stream);
                    }
                }
                catch
                {
                    // A missing or corrupt resource is not worth failing over:
                    // the form just keeps the default WinForms icon.
                }
                return _icon;
            }
        }

        /// <summary>Gives a form the application icon, if there is one.</summary>
        public static void Apply(Form form)
        {
            var icon = Value;
            if (icon != null) form.Icon = icon;
        }
    }
}
