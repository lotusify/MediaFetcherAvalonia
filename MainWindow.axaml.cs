using System.Diagnostics;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls; // Keep specific FluentAvalonia using
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization; // For parsing progress percentage
using System.IO;
using System.Linq; // Used in BuildArgs logic
using System.Text; // For StringBuilder potentially
using System.Threading;
using System.Threading.Tasks;

namespace MediaFetcherAvalonia
{
    public partial class MainWindow : Window
    {
        // --- Constants ---
        private readonly string[] _videoFormats = { "mp4", "webm", "mkv" };
        private readonly string[] _audioFormats = { "mp3", "m4a", "opus", "aac", "flac", "wav", "vorbis" };
        private const string YT_DLP_TOOL_NAME = "yt-dlp"; // Use constant for tool name
        private object? _lastSelectedItem;

        // --- State & Settings ---
        private readonly AppSettings _settings;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly List<string> _logBuffer = new List<string>();
        // private string _currentDownloadFile = string.Empty; // Removed as it wasn't used in UpdateProgress

        //---------------------------------------------------------------------
        // Constructor and Initialization
        //---------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            _settings = AppSettings.Load();

            SetupEventHandlers();
            InitializeUIState();
        }

        private void SetupEventHandlers()
        {
            this.Opened += (s, e) => UrlBox.Focus();
            NavView.SelectionChanged += NavigationView_SelectionChanged; // Keep the event handler subscription
            TypeCombo.SelectionChanged += (s, e) => UpdateFormatList();
            DownloadBtn.Click += DownloadBtn_Click;
            CancelBtn.Click += CancelBtn_Click; // Ensure CancelBtn exists and is named correctly in XAML
            SaveSettingsBtn.Click += SaveSettingsBtn_Click;
            BrowseDirectoryBtn.Click += BrowseDirectoryBtn_Click;
            UrlBox.KeyDown += UrlBox_KeyDown;
        }

        private void InitializeUIState()
        {
            // *** NavigationView ***
            // Set the initial selected item
            NavView.SelectedItem = NavView.MenuItems.FirstOrDefault() ?? NavView.MenuItems[0]; // Safer selection
            // Directly update the view based on the initial selection
            UpdateMainView(NavView.SelectedItem); // <-- MODIFIED: Call helper directly

            // *** Media Type / Format ***
            TypeCombo.SelectedIndex = 0;
            UpdateFormatList(); // Includes setting FormatCombo.SelectedIndex

            // *** Resolution ***
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
            UpdateResolutionComboState(); // Set initial enabled state

            // *** Settings Page ***
            InitializeSettingsPage();

             // *** Initial Button/Progress Bar State ***
            SetUIDownloadingState(false);
        }

        private void InitializeSettingsPage()
        {
            CustomOutputTemplateBox.Text = _settings.CustomOutputDirectory;
            CustomFileNameBox.Text = _settings.CustomFileNameTemplate;
            ErrorHandlingCombo.SelectedIndex = (int)_settings.ErrorHandling;
            CustomArgsBox.Text = _settings.CustomExtraArgs;
            PreferredLangBox.Text = _settings.PreferredLanguages;
            UpdateCurrentOutputDirectoryDisplay();
        }

        //---------------------------------------------------------------------
        // Event Handlers & UI Logic Helpers
        //---------------------------------------------------------------------

        // Handles actual changes triggered by user interaction or programmatic selection changes
        private void NavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
             var selectedItem = e.SelectedItem as NavigationViewItem;
                string? tag = selectedItem?.Tag?.ToString();
            
                if (tag == "GitHubLink")
                {
                    // --- Handle the GitHub Link ---
                    string githubUrl = "https://github.com/lotusify/MediaFetcherAvalonia"; // Your repo URL
            
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = githubUrl,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error opening GitHub URL: {ex.Message}");
                        // Optionally show error to user
                        // await ShowError($"Could not open link: {ex.Message}");
                    }
            
                    // --- IMPORTANT: Reset Selection ---
                    // Prevent navigation and reset selection back to the last actual page item
                    // Use Dispatcher to avoid issues modifying selection during the event
                    Dispatcher.UIThread.Post(() =>
                    {
                         if (NavView != null) // Ensure NavView is accessible
                         {
                              NavView.SelectedItem = _lastSelectedItem ?? NavView.MenuItems.FirstOrDefault(); // Fallback to first item if needed
                         }
                    }, DispatcherPriority.Background); // Use Background priority to run after current layout pass
            
                }
                else if (tag != null) // Handle regular page navigation
                {
                    // --- Your existing page navigation logic ---
                    // e.g., Navigate to different frames/views based on tag ("HomePage", "SettingsPage")
                     UpdateMainView(selectedItem); // Assuming this handles page switching
            
                     // --- Store the last *valid page* item selected ---
                     _lastSelectedItem = selectedItem;
                }
                // Handle cases where selectedItem or tag is null if necessary
            UpdateMainView(e.SelectedItem); // Call the common logic method
        }

        // Contains the logic to switch views based on the selected item
        private void UpdateMainView(object? selectedItem) // <-- NEW HELPER METHOD
        {
            bool isSettingsSelected = selectedItem == NavView.SettingsItem;
            string? tag = (selectedItem as NavigationViewItem)?.Tag?.ToString();

            // Determine which view to show
            if (isSettingsSelected || tag == "SettingsPage")
            {
                 HomePage.IsVisible = false;
                 SettingsPage.IsVisible = true;
            }
            else // Assume HomePage or default view
            {
                 HomePage.IsVisible = true;
                 SettingsPage.IsVisible = false;
            }
        }


        private void UpdateFormatList()
        {
            if (TypeCombo.SelectedItem is not ComboBoxItem selectedTypeItem || selectedTypeItem.Tag == null) return;

            var tag = selectedTypeItem.Tag.ToString();

            if (tag == "audio")
            {
                FormatCombo.ItemsSource = _audioFormats;
            }
            else // video or videoNoAudio
            {
                FormatCombo.ItemsSource = _videoFormats;
            }
            FormatCombo.SelectedIndex = 0; // Default to first format in the new list
            UpdateResolutionComboState(); // Update resolution enabled state
        }

        private void UpdateResolutionComboState()
        {
             if (TypeCombo.SelectedItem is not ComboBoxItem selectedTypeItem || selectedTypeItem.Tag == null) return;
             var tag = selectedTypeItem.Tag.ToString();
             // Only enable if not audio AND not currently downloading
             bool isDownloading = !DownloadBtn.IsEnabled; // Infer download state from button
             ResolutionCombo.IsEnabled = (tag != "audio") && !isDownloading;
        }

        private async void BrowseDirectoryBtn_Click(object? sender, RoutedEventArgs e)
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null)
            {
                await ShowError("Cannot access storage provider.");
                return;
            }

            // Determine starting directory
            string startDirPath = !string.IsNullOrWhiteSpace(_settings.CustomOutputDirectory)
                                ? _settings.CustomOutputDirectory
                                : AppSettings.DefaultDownloadsPath;

            // Ensure start directory exists or fallback
            IStorageFolder? startDir = null;
            try
            {
                 if (!Directory.Exists(startDirPath)) Directory.CreateDirectory(startDirPath);
                 startDir = await storageProvider.TryGetFolderFromPathAsync(startDirPath);
            }
            catch (Exception ex)
            {
                 Log($"Warning: Could not access or create start directory '{startDirPath}'. Falling back. Error: {ex.Message}");
                 // Fallback to user's documents or home if possible
                 var fallbackDir = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                                ?? await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures); // Or another sensible default
                 startDir = fallbackDir;
            }


            var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Output Directory",
                AllowMultiple = false,
                SuggestedStartLocation = startDir // Use the determined start directory
            });

            if (folder != null && folder.Count > 0)
            {
                // Use Path property if available, otherwise fallback to trying to get path
                 string? selectedPath = folder[0].TryGetLocalPath(); // Avalonia helper

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    CustomOutputTemplateBox.Text = selectedPath;
                    _settings.CustomOutputDirectory = selectedPath;
                    UpdateCurrentOutputDirectoryDisplay();
                }
                 else
                 {
                     await ShowError("Could not get a local path for the selected folder.");
                 }
            }
        }

        private void SaveSettingsBtn_Click(object? sender, RoutedEventArgs e)
        {
            _settings.CustomOutputDirectory = CustomOutputTemplateBox.Text?.Trim() ?? string.Empty;
            _settings.CustomFileNameTemplate = CustomFileNameBox.Text?.Trim() ?? AppSettings.DefaultFileNameTemplate; // Use default if empty
            _settings.ErrorHandling = (ErrorHandlingMode)ErrorHandlingCombo.SelectedIndex;
            _settings.CustomExtraArgs = CustomArgsBox.Text?.Trim() ?? string.Empty;
            _settings.PreferredLanguages = PreferredLangBox.Text?.Trim() ?? "";
            _settings.Save();
            UpdateCurrentOutputDirectoryDisplay();
            Log("Settings saved successfully.");
            // Optionally navigate back: NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void UrlBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DownloadBtn.IsEnabled)
            {
                DownloadBtn_Click(DownloadBtn, new RoutedEventArgs());
                e.Handled = true; // Prevent further processing of the Enter key
            }
        }

         private void CancelBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                // Log("Attempting to cancel download...");
                _cancellationTokenSource.Cancel();
                // UI state update (like disabling cancel button) will happen in SetUIDownloadingState called from finally block
            }
            else
            {
                // Log("Cancellation source not active or already cancelled.");
            }
        }

        //---------------------------------------------------------------------
        // Download Logic
        //---------------------------------------------------------------------

        private async void DownloadBtn_Click(object? sender, RoutedEventArgs e)
        {
            var url = UrlBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                await ShowError("Please enter a URL.");
                return;
            }

            // Dispose previous CTS and create a new one
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            SetUIDownloadingState(true);
            LogBox.Text = ""; // Clear log display
            _logBuffer.Clear();

            try
            {
                 await StartDownloadAsync(url, cancellationToken); // Call the refactored method

                // Log completion status (only if not cancelled)
                if (!cancellationToken.IsCancellationRequested)
                {
                    Log("Download process finished.");
                }
            }
            catch (OperationCanceledException) // Catch cancellation specifically
            {
                Log("Download process was cancelled by user.");
            }
            catch (FileNotFoundException fnfEx) // Catch specific file not found for yt-dlp
            {
                Log($"ERROR: Required tool not found - {fnfEx.Message}");
                 // ShowError was likely called already when GetToolPath returned null
            }
            catch (Exception ex) // Catch other unexpected errors during setup or execution
            {
                Log($"\n--- UNEXPECTED ERROR ---");
                Log($"Message: {ex.Message}");
                Log($"Type: {ex.GetType().FullName}");
                #if DEBUG
                Log($"Stack Trace: {ex.StackTrace}");
                #endif
                Log($"--- END ERROR ---");
                await ShowError($"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure UI is always reset and CTS is cleaned up
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                SetUIDownloadingState(false);
            }
        }

        private async Task StartDownloadAsync(string url, CancellationToken cancellationToken)
        {
             // 1. Find yt-dlp Path (using the corrected ExternalTools)
            string? ytDlpPath = ExternalTools.GetToolPath(YT_DLP_TOOL_NAME);

            if (string.IsNullOrEmpty(ytDlpPath))
            {
                // Log the error and throw FileNotFoundException which will be caught by the caller
                string errorMsg = $"{YT_DLP_TOOL_NAME} executable not found in application directory or system PATH.";
                Log($"ERROR: {errorMsg}");
                Log("Please ensure it's installed and its location is included in the PATH environment variable.");
                 await ShowError($"{errorMsg}\nPlease ensure it's installed and in the system PATH."); // Show error before throwing
                throw new FileNotFoundException(errorMsg, YT_DLP_TOOL_NAME);
            }

            Log($"Starting download using {YT_DLP_TOOL_NAME} found at: {ytDlpPath}");

            // 2. Build Arguments
            string args = BuildArgs(url); // Pass the validated URL
            Log($"Arguments: {args}"); // Log the arguments being used

             // 3. Run the Process
             // No need for File.Exists check here, GetToolPath already did it.
             await RunProcessAsync(ytDlpPath, args, cancellationToken);
        }


        private string BuildArgs(string url)
        {
            // Use StringBuilder for efficiency if many parts are added conditionally
            var argsBuilder = new StringBuilder();

            // Playlist handling
            argsBuilder.Append(PlaylistCheck.IsChecked == true ? "--yes-playlist" : "--no-playlist");

            // Format/Resolution/Force
            if (TypeCombo.SelectedItem is not ComboBoxItem selectedTypeItem || selectedTypeItem.Tag == null ||
                FormatCombo.SelectedItem == null || ResolutionCombo.SelectedItem is not Resolution selectedRes)
            {
                 Log("Warning: Could not determine selected format/resolution. Using defaults.");
                 // Handle default case or throw error if selection is mandatory
                 // For now, just append URL and return
                 argsBuilder.Append($" \"{url}\""); // Basic quoting for URL
                 return argsBuilder.ToString();
            }

            var typeTag = selectedTypeItem.Tag.ToString()!;
            var format = FormatCombo.SelectedItem.ToString()!;
            var resolutionValue = selectedRes.Value;
            var forceFormat = ForceCheck.IsChecked == true;

            string formatSelector = GetFormatSelector(typeTag, resolutionValue);
            argsBuilder.Append($" --format \"{formatSelector}\""); // Add format selector

            // Add audio/merge options based on type
            if (typeTag == "audio")
            {
                argsBuilder.Append(" --extract-audio");
                if (forceFormat)
                {
                    argsBuilder.Append($" --audio-format {format}");
                }
            }
            else if (typeTag == "videoNoAudio")
            {
                if (forceFormat)
                {
                    argsBuilder.Append($" --recode-video {format}");
                }
            }
            else if (typeTag == "video")
            {
                if (forceFormat)
                {
                    argsBuilder.Append($" --merge-output-format {format}");
                }
            }

            // Error Handling
            switch (_settings.ErrorHandling)
            {
                case ErrorHandlingMode.IgnoreErrors:
                    argsBuilder.Append(" --ignore-errors");
                    break;
                case ErrorHandlingMode.AbortOnErrors:
                    argsBuilder.Append(" --abort-on-error");
                    break;
                 // Default: ErrorHandlingMode.None - add no flags
            }

            // Output Path and Filename Template
            string outputDir = GetEnsuredOutputDirectory();
            string filenameTemplate = string.IsNullOrWhiteSpace(_settings.CustomFileNameTemplate)
                                        ? AppSettings.DefaultFileNameTemplate
                                        : _settings.CustomFileNameTemplate;
            string outputTemplate = Path.Combine(outputDir, filenameTemplate);

            // Basic quoting for the output path template
            argsBuilder.Append($" --output \"{outputTemplate}\"");
            
            // Encoding UTF-8 to resolve Unicode characters issue (like Vietnamese characters)
            argsBuilder.Append(" --encoding utf-8");
            
            // Preferred Languages
            if (!string.IsNullOrWhiteSpace(_settings.PreferredLanguages))
            {
                // split on commas, trim out empty entries
                var langs = _settings.PreferredLanguages
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim());

                foreach (var lang in langs)
                {
                    // Note the leading space—keeps us from concatenating words together
                    argsBuilder.Append($" --extractor-args \"youtube:lang={lang}\"");
                }
            }
            
            if (!string.IsNullOrWhiteSpace(_settings.CustomExtraArgs))
            {
                // Append a space before adding the extra args
                argsBuilder.Append($" {_settings.CustomExtraArgs}"); 
            }
            
            // Finally, the URL (ensure basic quoting)
            argsBuilder.Append($" \"{url}\"");

            return argsBuilder.ToString();
        }

        // Helper to get the format selector string for yt-dlp
        private string GetFormatSelector(string typeTag, string resolutionValue)
        {
             string videoSelector = "bestvideo";
             string audioSelector = "bestaudio";
             string fallback = "/best"; // Fallback if specific streams not found

            if (resolutionValue != "best" && resolutionValue != "worst")
            {
                 // Apply height constraint if a specific resolution (not best/worst) is chosen
                 videoSelector += $"[height<={resolutionValue}]";
                 audioSelector += $"[height<={resolutionValue}]"; // Note: height constraint on audio might be ignored by yt-dlp but doesn't hurt
            }
             else if (resolutionValue == "worst")
             {
                 videoSelector = "worstvideo";
                 audioSelector = "worstaudio";
                 fallback = "/worst";
             }
             // If resolutionValue == "best", we use the defaults "bestvideo", "bestaudio", "/best"

             // Construct the final selector based on the media type tag
             if (typeTag == "audio")
             {
                 return audioSelector; // For --extract-audio, yt-dlp primarily needs audio selection hint
             }
             else if (typeTag == "videoNoAudio")
             {
                 return videoSelector; // Only download video
             }
             else // Default: video (with audio)
             {
                 return $"{videoSelector}+{audioSelector}{fallback}"; // Combine video and audio
             }
        }

        // Helper to get and ensure the output directory exists
        private string GetEnsuredOutputDirectory()
        {
             string outputDir = !string.IsNullOrWhiteSpace(_settings.CustomOutputDirectory)
                                ? _settings.CustomOutputDirectory
                                : AppSettings.DefaultDownloadsPath;
             try
             {
                 if (!Directory.Exists(outputDir))
                 {
                     Log($"Creating output directory: {outputDir}");
                     Directory.CreateDirectory(outputDir);
                 }
             }
             catch (Exception ex)
             {
                 Log($"Warning: Could not create or access output directory '{outputDir}'. Falling back to default. Error: {ex.Message}");
                 outputDir = AppSettings.DefaultDownloadsPath;
                 try
                 {
                      Directory.CreateDirectory(outputDir); // Try creating default
                 }
                 catch (Exception innerEx)
                 {
                      Log($"ERROR: Could not create default downloads directory '{outputDir}'. Error: {innerEx.Message}");
                      // Consider throwing or returning a known invalid path to halt the process
                      // For now, return the path even if creation failed, yt-dlp might handle it or fail.
                 }
             }
             return outputDir;
        }

        private Task RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var psi = new ProcessStartInfo(fileName) // Arguments added separately for clarity/safety
            {
                // Arguments = arguments, // Set below using ArgumentList for better handling
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppContext.BaseDirectory, // Or GetEnsuredOutputDirectory() if preferred
                StandardOutputEncoding = new UTF8Encoding(false), // Use UTF-8 without BOM
                StandardErrorEncoding = new UTF8Encoding(false),
                // Set console output encoding to UTF-8 for yt-dlp
                EnvironmentVariables = { ["PYTHONIOENCODING"] = "utf-8" }
            };

            // Use ArgumentList for potentially safer handling of spaces/quotes,
            // though yt-dlp args are complex. Simple joining might be okay if BuildArgs handles quoting.
            // For simplicity here, we stick with passing the pre-formatted string.
             psi.Arguments = arguments;


            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            // --- Cancellation Registration ---
            var cancellationRegistration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        Log("Cancellation requested. Terminating process...");
                        proc.Kill(entireProcessTree: true); // Attempt to kill the process and its children
                        // Don't TrySetCanceled here immediately, let Exited handler do it
                    }
                }
                catch (InvalidOperationException) { /* Process already exited */ }
                catch (Exception ex)
                {
                    Log($"Error during process cancellation: {ex.Message}");
                    // If killing failed, still try to cancel the Task
                    tcs.TrySetCanceled(cancellationToken);
                }
            });
            // --- End Cancellation ---

            proc.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null && !cancellationToken.IsCancellationRequested)
                {
                    ProcessOutput(e.Data, isError: false, cancellationToken);
                }
            };

            proc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null && !cancellationToken.IsCancellationRequested)
                {
                    ProcessOutput(e.Data, isError: true, cancellationToken);
                }
            };

            proc.Exited += (s, e) =>
            {
                // Ensure remaining output/error is processed if needed (though BeginRead should handle most)
                // Check exit code? proc.ExitCode

                cancellationRegistration.Dispose(); // Clean up registration

                // Determine if the task completed successfully, was cancelled, or failed
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                // else if (proc.ExitCode != 0) // Optional: Check exit code for failure
                // {
                //     tcs.TrySetException(new Exception($"Process exited with code {proc.ExitCode}"));
                // }
                else
                {
                    tcs.TrySetResult(true); // Signal normal completion
                }
                proc.Dispose(); // Dispose the process object
            };

            // --- Start Process ---
            try
            {
                 // Check cancellation *just before* starting
                 if (cancellationToken.IsCancellationRequested)
                 {
                      cancellationRegistration.Dispose();
                      tcs.SetCanceled(cancellationToken);
                      proc.Dispose();
                      return tcs.Task;
                 }
                 
                if (!proc.Start())
                {
                     throw new InvalidOperationException("Failed to start process.");
                }
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log($"ERROR starting process: {ex.Message}");
                cancellationRegistration.Dispose();
                tcs.TrySetException(ex); // Signal task failed due to start error
                proc.Dispose();
            }
            // --- End Start Process ---

            return tcs.Task;
        }

         // Centralized output processing
        private void ProcessOutput(string line, bool isError, CancellationToken cancellationToken)
        {
             if (cancellationToken.IsCancellationRequested) return; // Extra check

              // Keep the original string intact to preserve any unicode characters
              if (string.IsNullOrEmpty(line)) return;
         
              // Ensure we have valid UTF-8 content
              // Try parsing progress first
              if (!isError && TryParseAndUpdateProgress(line))
              {
                  // Progress was handled, don't log it as a normal line
                  return;
              }
              
              // If not progress or it's an error line, log it
              // Post to UI thread to ensure proper thread marshaling for UI updates
              Dispatcher.UIThread.Post(() => Log(isError ? $"ERROR: {line}" : line));
        }

        //---------------------------------------------------------------------
        // UI Update Methods
        //---------------------------------------------------------------------

        private void SetUIDownloadingState(bool isDownloading)
        {
            // Use Dispatcher for safety, although often called from UI thread already
            Dispatcher.UIThread.Post(() =>
            {
                DownloadBtn.IsEnabled = !isDownloading;
                CancelBtn.IsEnabled = isDownloading;

                if (DownloadProgressBar != null)
                {
                    DownloadProgressBar.IsVisible = isDownloading;
                    DownloadProgressBar.IsIndeterminate = isDownloading; // Keep indeterminate for now
                    if (!isDownloading) DownloadProgressBar.Value = 0;
                }

                // Disable input controls during download
                UrlBox.IsEnabled = !isDownloading;
                TypeCombo.IsEnabled = !isDownloading;
                FormatCombo.IsEnabled = !isDownloading;
                PlaylistCheck.IsEnabled = !isDownloading;
                ForceCheck.IsEnabled = !isDownloading;
                // ResolutionCombo enabled state depends on TypeCombo AND download state
                UpdateResolutionComboState(); // Re-evaluate based on TypeCombo
                // Ensure disabled if downloading, overriding UpdateResolutionComboState if needed
                if (isDownloading) ResolutionCombo.IsEnabled = false;


                // Optionally disable settings navigation/controls
                // SettingsPage.IsEnabled = !isDownloading;
                // NavView.IsEnabled = !isDownloading;
            });
        }

        // Tries to parse yt-dlp progress line and update UI. Returns true if successful.
        private bool TryParseAndUpdateProgress(string line)
        {
            // Example line: "[download]  1.5% of ~615.83MiB at 27.18MiB/s ETA 00:22"
            if (!line.Contains("[download]") || !line.Contains('%')) return false;

            try
            {
                int percentIndex = line.IndexOf('%');
                if (percentIndex < 0) return false;

                // Find the start of the percentage number
                int startIndex = line.LastIndexOf(' ', percentIndex - 1) + 1;
                if (startIndex < 0 || startIndex >= percentIndex) return false;

                string percentString = line.Substring(startIndex, percentIndex - startIndex).Trim();

                if (double.TryParse(percentString, NumberStyles.Float, CultureInfo.InvariantCulture, out double percentage))
                {
                    // Update UI on the UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (DownloadProgressBar != null)
                        {
                            DownloadProgressBar.IsIndeterminate = false; // Now we have a value
                            DownloadProgressBar.Value = percentage;
                        }
                        // Update the log buffer with the original line to preserve Unicode characters
                        UpdateProgressLogLine(line);
                    });
                    return true; // Successfully parsed and updated progress
                }
            }
            catch (Exception ex)
            {
                Log($"Warning: Failed to parse progress line '{line}'. Error: {ex.Message}");
            }
            return false; // Parsing failed
        }

        // Updates the log buffer, replacing the last download progress line if found
        private void UpdateProgressLogLine(string progressText)
        {
            // Find the index of the last line that looks like a progress update
            int lastProgressIndex = -1;
            for (int i = _logBuffer.Count - 1; i >= 0; i--)
            {
                if (_logBuffer[i].Contains("[download]") && _logBuffer[i].Contains('%'))
                {
                    lastProgressIndex = i;
                    break;
                }
            }

            if (lastProgressIndex != -1)
            {
                // Replace the last progress line
                _logBuffer[lastProgressIndex] = progressText;
            }
            else
            {
                // If no previous progress line found, just add it
                _logBuffer.Add(progressText);
            }

            // Update the visible TextBox
            LogBox.Text = string.Join("\n", _logBuffer);
            LogBox.CaretIndex = LogBox.Text.Length; // Scroll to end
        }


        // Logs a message to the buffer and updates the TextBox
        private void Log(string text)
        {
             // Ensure called on UI thread if potentially called from background
             // Dispatcher.UIThread.Post(() => { ... }); // Use if necessary, but most calls seem safe

            _logBuffer.Add(text);
            LogBox.Text = string.Join("\n", _logBuffer);
            LogBox.CaretIndex = LogBox.Text.Length; // Scroll to end
        }

        private void UpdateCurrentOutputDirectoryDisplay()
        {
            string outputDir = GetEnsuredOutputDirectory(); // Use helper to get/create dir
        }

        // Simple error dialog
        private async Task ShowError(string message)
        {
             // Ensure on UI thread
             await Dispatcher.UIThread.InvokeAsync(async () =>
             {
                 var dialog = new ContentDialog
                 {
                     Title = "Error",
                     Content = message,
                     PrimaryButtonText = "OK"
                 };
                 // Ensure the dialog is shown relative to the main window if possible
                 if (VisualRoot is Window mainWindow)
                 {
                      await dialog.ShowAsync(mainWindow);
                 }
                 else
                 {
                      await dialog.ShowAsync(); // Show without owner if necessary
                 }
             });
        }

        private void LanguageListLinkButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string url && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                try
                {
                    // Use Process.Start to open the URL in the default browser
                    // Need to configure ProcessStartInfo for cross-platform compatibility
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true // Important for opening URLs in the default browser
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    // Handle potential errors (e.g., browser not found, invalid URL format after check)
                    Debug.WriteLine($"Error opening URL: {ex.Message}");
                    // Optionally show an error message to the user
                    // await ShowError($"Could not open link: {ex.Message}");
                }
            }
        }
    }
}
