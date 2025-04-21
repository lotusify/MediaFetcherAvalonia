using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace MediaFetcherAvalonia
{
    public static class MicaHelper
    {
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && 
                Environment.OSVersion.Version.Build >= 22000;

        public static void TrySetMicaBackdrop(Window window)
        {
            if (!IsSupported)
                return;

            // Get platform-specific window handle
            var platformHandle = window.TryGetPlatformHandle();
            if (platformHandle == null)
                return;

            // Try to apply Mica effect
            var hwnd = platformHandle.Handle;
            if (hwnd != IntPtr.Zero)
            {
                var darkMode = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
                TrySetWindowBackdrop(hwnd, darkMode);
            }
        }

        private static void TrySetWindowBackdrop(IntPtr hwnd, bool darkMode)
        {
            // Enable Dark Mode if needed
            if (darkMode)
            {
                int darkModeValue = 1;
                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20
                DwmSetWindowAttribute(hwnd, 20, ref darkModeValue, sizeof(int));
            }
        
            try
            {
                // Windows 11 22H2+: Use DWM_SYSTEMBACKDROP_TYPE values
                // DWMWA_SYSTEMBACKDROP_TYPE = 38
                // DWMSBT_MAINWINDOW = 2 (standard Mica)
                int backdropType = 2;
                DwmSetWindowAttribute(hwnd, 38, ref backdropType, sizeof(int));
            }
            catch
            {
                // Fallback for older Windows 11 versions
                // DWMWA_MICA_EFFECT = 1029
                int micaEffect = 1;
                try
                {
                    DwmSetWindowAttribute(hwnd, 1029, ref micaEffect, sizeof(int));
                }
                catch
                {
                    // Silently fail if this isn't supported
                }
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);
    }
}
