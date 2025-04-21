using System;
using System.IO;

namespace MediaFetcherAvalonia
{
    public static class ExternalTools
    {
        /// <summary>
        /// Resolves the path to an external tool, checking the application directory first,
        /// then falling back to system PATH if not found locally.
        /// </summary>
        /// <param name="toolName">Name of the tool (e.g., "yt-dlp" or "ffmpeg")</param>
        /// <returns>Full path to the tool if found in app directory, or just the tool name if not</returns>
        public static string GetToolPath(string toolName)
        {
            // First try to find the tool in the same directory as our application
            string localPath = Path.Combine(AppContext.BaseDirectory, toolName);
            
            // On Windows, also try with .exe extension if not already included
            if (OperatingSystem.IsWindows() && !toolName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                string localPathExe = Path.Combine(AppContext.BaseDirectory, toolName + ".exe");
                if (File.Exists(localPathExe))
                    return localPathExe;
            }

            // Check if the tool exists in the local path
            if (File.Exists(localPath))
                return localPath;

            // If not found locally, return just the tool name (will use system PATH)
            return toolName;
        }
    }
}
