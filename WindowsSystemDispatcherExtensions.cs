using Avalonia;
using Avalonia.Controls;
using System;

namespace MediaFetcherAvalonia
{
    public static class WindowsSystemDispatcherExtensions
    {
        /// <summary>
        /// Try to set Mica effect on the Window after it's opened
        /// </summary>
        public static void EnableMicaIfSupported(this Window window)
        {
            // Subscribe to Opened event to ensure the window is ready 
            // when we try to apply the Mica effect
            window.Opened += (_, _) =>
            {
                MicaHelper.TrySetMicaBackdrop(window);
            };

            // Also handle theme changes to update the Mica effect
            // if the user switches between light and dark mode
            if (Application.Current != null)
            {
                Application.Current.ActualThemeVariantChanged += (_, _) =>
                {
                    MicaHelper.TrySetMicaBackdrop(window);
                };
            }
        }
    }
}
