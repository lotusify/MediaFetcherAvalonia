using System;
using System.IO;
using System.Text.Json;

namespace MediaFetcherAvalonia
{
    public enum ErrorHandlingMode
    {
        None,
        IgnoreErrors,
        AbortOnErrors
    }
    
    public class AppSettings
    {
        public string CustomOutputDirectory { get; set; } = string.Empty;
        public string CustomFileNameTemplate { get; set; } = "%(title)s.%(ext)s";
        public static string DefaultFileNameTemplate { get; set; } = "%(title)s.%(ext)s";
        
        public string CustomExtraArgs { get; set; } = string.Empty;
        
        public string PreferredLanguages { get; set; } = "";
        public ErrorHandlingMode ErrorHandling { get; set; } = ErrorHandlingMode.None;
        
        // For backwards compatibility with existing settings files
        public bool IgnoreErrors 
        { 
            get => ErrorHandling == ErrorHandlingMode.IgnoreErrors;
            set { if (value) ErrorHandling = ErrorHandlingMode.IgnoreErrors; }
        }
        
        public bool AbortOnErrors 
        { 
            get => ErrorHandling == ErrorHandlingMode.AbortOnErrors;
            set { if (value) ErrorHandling = ErrorHandlingMode.AbortOnErrors; }
        }
        
        // Returns the default downloads folder path
        public static string DefaultDownloadsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "MediaFetcher");
            
        // Path to settings file
        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MediaFetcher", "settings.json");
            
        // Save settings to disk
        public void Save()
        {
            try
            {
                string? dirPath = Path.GetDirectoryName(SettingsFilePath);
                if (dirPath != null && !Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                    
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Silently fail if we can't save settings
            }
        }
        
        // Load settings from disk
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                    return settings ?? new AppSettings();
                }
            }
            catch
            {
                // Silently fail if we can't load settings
            }
            
            return new AppSettings();
        }
    }
}
