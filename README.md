<img src="Assets/MediaFetcher.ico" alt="MediaFetcherAvalonia Icon" width="64"/>
# MediaFetcherAvalonia

A desktop application built with Avalonia (a cross-platform UI framework for .NET) that acts as a graphical frontend for the popular command-line media downloader `yt-dlp`.

## Description

MediaFetcherAvalonia provides a user-friendly interface to download video and audio from various websites supported by `yt-dlp`. It allows users to easily select formats, resolutions, and other options without needing to use the command line directly.

## Features

* **URL Input**: Simple text box to paste the URL of the media you want to download.
* **Media Type Selection**: Choose between downloading:
    * Video with Audio (default)
    * Video Only
    * Audio Only
* **Format Selection**:
    * Video Formats: mp4, webm, mkv
    * Audio Formats: mp3, m4a, opus, aac, flac, wav, vorbis
* **Resolution Selection**: Choose video resolution from 144p up to 8K, including "Best" and "Worst" options.
* **Playlist Handling**: Option to download entire playlists or just the single video specified by the URL.
* **Force Format**: Option to force recoding or merging into the selected output format (requires `ffmpeg` to be available to `yt-dlp`).
* **Custom Output**:
    * Specify a custom output directory.
    * Define a custom filename template using `yt-dlp` syntax (default: `%(title)s.%(ext)s`).
* **Download Management**:
    * View real-time download progress and speed.
    * See detailed logs from `yt-dlp`.
    * Cancel ongoing downloads.
* **Error Handling**: Configure how download errors are handled (None, Ignore Errors, Abort on Errors).
* **Settings Persistence**: Saves configuration (output path, filename template, error handling) to a `settings.json` file.
* **Platform Features**: Includes basic support for the Mica backdrop effect on compatible Windows 11 systems.

## Requirements

1.  **.NET Runtime**: The specific version required depends on how the application was built, but a modern .NET Desktop Runtime (e.g., .NET 6, 7, or 8) is needed.
2.  **`yt-dlp`**: The core `yt-dlp` executable **must** be present. The application will look for it in:
    * The same directory as the `MediaFetcherAvalonia` executable.
    * Any directory listed in your system's PATH environment variable.
    You can download the latest `yt-dlp` from its official repository.
3.  **(Optional but Recommended) `ffmpeg`**: While not directly checked for by this application, `yt-dlp` often requires `ffmpeg` for merging separate video/audio streams or for format conversions (especially when using the "Force Format" option). Ensure `ffmpeg` is installed and accessible by `yt-dlp` (usually by placing it in the same directory as `yt-dlp` or adding it to the system PATH).

## Usage

1.  Ensure `yt-dlp` (and preferably `ffmpeg`) is installed and accessible.
2.  Run the `MediaFetcherAvalonia` executable.
3.  Paste the URL of the media you want to download into the URL box.
4.  Select the desired Media Type, Format, and Resolution.
5.  Check/uncheck Playlist and Force Format options as needed.
6.  Click the "Download" button.
7.  Monitor progress and logs in the text area below.
8.  Use the "Cancel" button to stop a download.
9.  Navigate to the "Settings" page (gear icon) to configure the output directory, filename template, and error handling. Click "Save Settings" to apply changes.


You can manually edit this file, but it's easier to use the Settings page within the application.

## Disclaimer

**Please Note:** This application was developed as a school project and large portions of the code were generated with the assistance of AI. (Sorry, my teacher!)
