using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;      // ← for Dispatcher.UIThread.Post
using Avalonia.Platform;       // For IAssetLoader
using Avalonia.Media;          // For Thickness
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAvalonia.UI.Controls;

namespace MediaFetcherAvalonia
{
    public partial class MainWindow : Window
    {
        // Video vs Audio formats
        private readonly string[] _videoFormats = { "mp4", "webm", "mkv" };
        private readonly string[] _audioFormats = { "mp3", "m4a", "opus", "aac", "flac", "wav", "vorbis" };
        
        // App settings
        private AppSettings _settings;
    
        public MainWindow()
        {
            InitializeComponent();
            
            // Load app settings
            _settings = AppSettings.Load();
    
            // Set focus to URL box when window is opened
            this.Opened += (s, e) => UrlBox.Focus();
            
            // *** NavigationView setup ***
            NavView.SelectedItem = NavView.MenuItems[0];
            NavView.SelectionChanged += (_, e) =>
            {
                var selected = ((NavigationViewItem)e.SelectedItem!).Tag!.ToString();
                HomePage.IsVisible     = selected == "HomePage";
                SettingsPage.IsVisible = selected == "SettingsPage";
            };
    
            // *** Media type / format filtering ***
            TypeCombo.SelectedIndex = 0;                          // default to first type
            TypeCombo.SelectionChanged += (_,__) => UpdateFormatList();
            UpdateFormatList();                                   // initial population
    
            // *** Resolution combo init ***
            ResolutionCombo.ItemsSource = new[]
            {
                new Resolution("Best Quality", "best"),
                new Resolution("8K (4320p)", "4320"),
                new Resolution("4K (2160p)", "2160"),
                new Resolution("QHD (1440p)", "1440"),
                new Resolution("Full HD (1080p)", "1080"),
                new Resolution("HD (720p)", "720"),
                new Resolution("SD (480p)", "480"),
                new Resolution("360p", "360"),
                new Resolution("240p", "240"),
                new Resolution("144p", "144"),
                new Resolution("Lowest Quality", "worst")
            };
            ResolutionCombo.SelectedIndex = 0;
            
            // Set initial state of ResolutionCombo based on default media type selection
            var initialTag = ((ComboBoxItem)TypeCombo.SelectedItem!).Tag!.ToString();
            ResolutionCombo.IsEnabled = initialTag != "audio";

            // *** Download/Fetch button handler ***
            DownloadBtn.Click += DownloadBtn_Click;
            
            // *** Settings initialization ***
            InitializeSettings();
            
            // *** Save settings button handler ***
            SaveSettingsBtn.Click += SaveSettingsBtn_Click;
        }

        private void InitializeSettings()
        {
            // Initialize settings UI with values from loaded settings
            CustomOutputTemplateBox.Text = _settings.CustomOutputDirectory;
            CustomFileNameBox.Text = _settings.CustomFileNameTemplate;
            
            // Set error handling combo box
            ErrorHandlingCombo.SelectedIndex = (int)_settings.ErrorHandling;
            
            // Initialize directory browser button
            BrowseDirectoryBtn.Click += BrowseDirectoryBtn_Click;
            
            // Update current output directory display
            UpdateCurrentOutputDirectoryDisplay();
        }

        private void UpdateCurrentOutputDirectoryDisplay()
        {
            // Show the directory where files will be saved
            string outputDir = !string.IsNullOrWhiteSpace(_settings.CustomOutputDirectory)
                ? _settings.CustomOutputDirectory
                : AppSettings.DefaultDownloadsPath;
                
            // Ensure the directory exists
            try
            {
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);
            }
            catch
            {
                outputDir = AppSettings.DefaultDownloadsPath;
                try 
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch
                {
                    // Last resort fallback
                    outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                }
            }
            
            CurrentOutputDirBlock.Text = $"Current directory: {outputDir}";
        }
        
        
        private async void BrowseDirectoryBtn_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Output Directory"
            };
            
            // Start from current directory if it exists
            string startDir = !string.IsNullOrWhiteSpace(_settings.CustomOutputDirectory) && 
                              Directory.Exists(_settings.CustomOutputDirectory)
                ? _settings.CustomOutputDirectory
                : AppSettings.DefaultDownloadsPath;
                
            try
            {
                if (!Directory.Exists(startDir))
                    Directory.CreateDirectory(startDir);
                dialog.Directory = startDir;
            }
            catch
            {
                // Use a safe default
                dialog.Directory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            
            string? result = await dialog.ShowAsync(this);
            
            if (!string.IsNullOrEmpty(result))
            {
                CustomOutputTemplateBox.Text = result;
                _settings.CustomOutputDirectory = result;
                UpdateCurrentOutputDirectoryDisplay();
            }
        }
        
        private void SaveSettingsBtn_Click(object? sender, RoutedEventArgs e)
        {
            // Update settings from UI
            _settings.CustomOutputDirectory = CustomOutputTemplateBox.Text ?? string.Empty;
            _settings.CustomFileNameTemplate = CustomFileNameBox.Text ?? "%(title)s.%(ext)s";
            
            // Update error handling from the combo box
            _settings.ErrorHandling = (ErrorHandlingMode)ErrorHandlingCombo.SelectedIndex;
            
            // Save settings to disk
            _settings.Save();
            
            // Update the current directory display
            UpdateCurrentOutputDirectoryDisplay();
            
            // Show confirmation
            Log("Settings saved successfully.");
            
            // Navigate back to home page
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void UpdateFormatList()
        {
            // Get the selected media type tag: "video", "audio", or "videoNoAudio"
            var tag = ((ComboBoxItem)TypeCombo.SelectedItem!).Tag!.ToString();
            
            // Update format list based on media type
            if (tag == "audio")
            {
                FormatCombo.ItemsSource = _audioFormats;
                // Disable resolution selection for audio-only mode
                ResolutionCombo.IsEnabled = false;
            }
            else
            {
                FormatCombo.ItemsSource = _videoFormats;
                // Enable resolution selection for video modes
                ResolutionCombo.IsEnabled = true;
            }

            FormatCombo.SelectedIndex = 0;  // choose the first format by default
        }

        private async void DownloadBtn_Click(object? sender, RoutedEventArgs e)
        {
            DownloadBtn.IsEnabled = false;
            LogBox.Text = "";

            var url = UrlBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                await ShowError("Please enter a URL.");
                DownloadBtn.IsEnabled = true;
                return;
            }

            var args = BuildArgs(url);
            string ytDlpPath = ExternalTools.GetToolPath("yt-dlp");
            
            // Make it VERY clear this is just a command preview
            Log("═════════════════ COMMAND PREVIEW ═════════════════");
            Log($"$ {ytDlpPath} {args}");
            Log("═══════════════════════════════════════════════════");
            Log("Starting execution...");

            try
            {
                // Add helpful information about execution environment
                Log($"Current directory: {System.IO.Directory.GetCurrentDirectory()}");
                Log($"Application directory: {AppContext.BaseDirectory}");

                // Check if yt-dlp exists at the resolved path
                bool fileExists = System.IO.File.Exists(ytDlpPath);
                Log($"yt-dlp file exists at '{ytDlpPath}': {fileExists}");

                if (!fileExists && !ytDlpPath.Equals("yt-dlp"))
                {
                    // If it's not at the resolved path and not just "yt-dlp" (which would use PATH)
                    Log($"ERROR: yt-dlp not found at path: {ytDlpPath}");
                    Log(
                        "Please make sure yt-dlp is installed in the application directory or available in your system PATH.");

                    // Check if yt-dlp.exe exists (Windows naming)
                    string ytDlpExePath = ytDlpPath + ".exe";
                    bool exeExists = System.IO.File.Exists(ytDlpExePath);
                    Log($"Checking for .exe version at '{ytDlpExePath}': {exeExists}");

                    // If the .exe version exists, use that instead
                    if (exeExists)
                    {
                        ytDlpPath = ytDlpExePath;
                        Log($"Using found executable: {ytDlpPath}");
                    }
                    else
                    {
                        DownloadBtn.IsEnabled = true;
                        return;
                    }
                }

                // Try to run the process with improved error handling
                await RunProcessAsync(ytDlpPath, args);
                Log("Download completed!");
            }
            catch (Exception ex)
            {
                Log($"\nError: {ex.Message}");
                Log($"Error type: {ex.GetType().FullName}");
                Log($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                DownloadBtn.IsEnabled = true;
            }
                    }

        private string BuildArgs(string url)
        {
            var parts = new System.Collections.Generic.List<string>();

            // playlist yes/no
            parts.Add(PlaylistCheck.IsChecked == true
                ? "--yes-playlist"
                : "--no-playlist");

            // format/resolution/force
            var typeTag = ((ComboBoxItem)TypeCombo.SelectedItem!).Tag!.ToString()!;
            var fmt     = FormatCombo.SelectedItem!.ToString()!;
            var res     = ((Resolution)ResolutionCombo.SelectedItem!).Value;
            var force   = ForceCheck.IsChecked == true;

            if (typeTag == "audio")
            {
                parts.Add("--format");
                parts.Add("bestaudio");
                parts.Add("--extract-audio");
                if (force)
                    parts.Add($"--audio-format {fmt}");
            }
            else
            {
                // video or videoNoAudio
                string selector;
                
                if (res == "worst")
                {
                    selector = typeTag == "videoNoAudio"
                        ? "worstvideo"
                        : "worstvideo+worstaudio/worst";
                }
                else
                {
                    selector = typeTag == "videoNoAudio"
                        ? "bestvideo"
                        : "bestvideo+bestaudio/best";
                        
                    if (res != "best")
                        selector = selector
                            .Replace("bestvideo", $"bestvideo[height<={res}]")
                            .Replace("bestaudio", $"bestaudio[height<={res}]");
                }

                parts.Add($"--format \"{selector}\"");
                if (force)
                    parts.Add($"--merge-output-format {fmt}");
            }

            // Handle error options from settings
            if (_settings.IgnoreErrors)
                parts.Add("--ignore-errors");
            else if (_settings.AbortOnErrors) // These are mutually exclusive
                parts.Add("--abort-on-error");

            // output path and filename template
            string outputPath;
            string filenameTemplate = _settings.CustomFileNameTemplate;
            
            // If custom output directory is set, use it
            if (!string.IsNullOrWhiteSpace(_settings.CustomOutputDirectory))
            {
                outputPath = _settings.CustomOutputDirectory;
                // Ensure directory exists
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch
                {
                    // If we can't create directory, fall back to default
                    outputPath = AppSettings.DefaultDownloadsPath;
                    Directory.CreateDirectory(outputPath);
                }
            }
            else
            {
                // Use default downloads folder
                outputPath = AppSettings.DefaultDownloadsPath;
                Directory.CreateDirectory(outputPath);
            }
            
            // Combine path and filename template
            string outputTemplate = Path.Combine(outputPath, filenameTemplate);
            parts.Add($"--output \"{outputTemplate}\"");

            // finally the URL
            parts.Add(url);

            return string.Join(" ", parts);
        }

        // New: Create a list to store log lines
        private readonly List<string> _logBuffer = new List<string>();
        private string _currentDownloadFile = string.Empty;
        
        private Task RunProcessAsync(string fileName, string arguments)
        {
            // Process starts with the filename provided (which should already be resolved)
            var tcs = new TaskCompletionSource<bool>();
            
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                WindowStyle            = ProcessWindowStyle.Hidden,
                // Add working directory to ensure proper execution context
                WorkingDirectory       = AppContext.BaseDirectory
            };
            
            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        
            // Handle output data
            proc.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    string lineText = e.Data.Trim();
                    
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(lineText))
                        return;
                        
                    // Detect destination lines to track the current file
                    if (lineText.Contains("[download] Destination:"))
                    {
                        _currentDownloadFile = lineText;
                    }
                    
                    // Process progress lines differently
                    if (lineText.Contains("[download]") && lineText.Contains("%"))
                    {
                        Dispatcher.UIThread.Post(() => UpdateProgress(lineText));
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => Log(lineText));
                    }
                }
            };
            
            // Handle error data
            proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Dispatcher.UIThread.Post(() => Log($"ERROR: {e.Data}"));
                }
            };
            
            // Handle process exit
            proc.Exited += (s, e) => 
            {
                tcs.SetResult(true);
            };
        
            // Clear log buffer when starting a new process
            _logBuffer.Clear();
            _currentDownloadFile = string.Empty;
            LogBox.Text = string.Empty;
            
            try
            {
                Log($"Running process: {fileName} {arguments}");
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log($"ERROR starting process: {ex.Message}");
                Log($"Exception type: {ex.GetType().FullName}");
                tcs.SetException(ex);
            }
        
            return tcs.Task;
        }
        
        private void UpdateProgress(string progressText)
        {
            // Find if there's already a progress line for this file
            bool updatedExisting = false;
            
            for (int i = 0; i < _logBuffer.Count; i++)
            {
                // If this is a progress line for the current file
                if (_logBuffer[i].Contains("[download]") && 
                    _logBuffer[i].Contains("%") && 
                    !string.IsNullOrEmpty(_currentDownloadFile))
                {
                    // Replace it with the new progress
                    _logBuffer[i] = progressText;
                    updatedExisting = true;
                    break;
                }
            }
            
            // If we didn't update an existing line, add this as a new line
            if (!updatedExisting)
            {
                _logBuffer.Add(progressText);
            }
            
            // Update the text box
            LogBox.Text = string.Join("\n", _logBuffer);
            
            // Scroll to the bottom
            LogBox.CaretIndex = LogBox.Text.Length;
        }
        
        private void Log(string text)
        {
            // Add the text to our buffer
            _logBuffer.Add(text);
            
            // Update the text box with the full buffer
            LogBox.Text = string.Join("\n", _logBuffer);
            
            // Scroll to the bottom
            LogBox.CaretIndex = LogBox.Text.Length;
        }

        private async Task ShowError(string msg)
        {
            var dlg = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 100
            };
            var txt = new TextBlock
            {
                Text = msg,
                Margin = new Thickness(10)
            };
            dlg.Content = txt;
            await dlg.ShowDialog(this);
        }
        
        // Process Enter key in URL box to trigger fetch
        private void UrlBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter && DownloadBtn.IsEnabled)
            {
                DownloadBtn_Click(DownloadBtn, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}