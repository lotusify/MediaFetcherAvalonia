using System;
using System.IO;
using System.Linq; // Required for Environment.GetEnvironmentVariable and Split
using System.Runtime.InteropServices; // Required for OperatingSystem checks

namespace MediaFetcherAvalonia
{
    public static class ExternalTools
    {
        /// <summary>
        /// Resolves the full path to an external tool.
        /// Checks the application directory first, then searches the system PATH.
        /// </summary>
        /// <param name="toolName">Name of the tool (e.g., "yt-dlp" or "ffmpeg"). Can include .exe or not.</param>
        /// <returns>Full path to the tool if found, otherwise null.</returns>
        public static string? GetToolPath(string toolName)
        {
            // Determine the actual executable name (add .exe on Windows if needed)
            string executableName = toolName;
            if (OperatingSystem.IsWindows() && !toolName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                executableName = toolName + ".exe";
            }
            else if (!OperatingSystem.IsWindows() && toolName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // On non-Windows, remove .exe if present, as PATH usually won't include it
                 executableName = toolName.Substring(0, toolName.Length - 4);
            }


            // 1. Check application's base directory
            string localPath = Path.Combine(AppContext.BaseDirectory, executableName);
            if (File.Exists(localPath))
            {
                return localPath;
            }
             // Also check the original toolName in local path if it differs from executableName (e.g., user provided yt-dlp.exe on Linux)
            if (toolName != executableName) {
                string localOriginalPath = Path.Combine(AppContext.BaseDirectory, toolName);
                 if (File.Exists(localOriginalPath))
                 {
                      return localOriginalPath;
                 }
            }


            // 2. Check system PATH environment variable
            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (pathVariable != null)
            {
                var paths = pathVariable.Split(Path.PathSeparator);
                foreach (var path in paths)
                {
                    // Check using the potentially modified executable name first
                    string fullPath = Path.Combine(path, executableName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                     // If different, also check using the original tool name provided by the caller
                     if (toolName != executableName) {
                        string fullOriginalPath = Path.Combine(path, toolName);
                        if (File.Exists(fullOriginalPath))
                        {
                             return fullOriginalPath;
                        }
                     }
                }
            }

            // 3. If not found anywhere, return null (or consider throwing an exception)
             // Returning the base tool name here won't work with UseShellExecute = false.
            return null;
        }
    }
}