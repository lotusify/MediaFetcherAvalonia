using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Linq;
using FluentAvalonia.Styling;

namespace MediaFetcherAvalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }
    
            // Get the FluentAvaloniaTheme from styles
            var faTheme = this.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
            if (faTheme != null)
            {
                faTheme.PreferUserAccentColor = true;
                faTheme.PreferSystemTheme = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex}");
            // Show error dialog if possible
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && 
                desktop.MainWindow == null)
            {
                desktop.MainWindow = new Window
                {
                    Title = "Initialization Error",
                    Content = new TextBlock
                    {
                        Text = $"Error starting application: {ex.Message}",
                        Margin = new Avalonia.Thickness(20),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    Width = 400,
                    Height = 200
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}