namespace MediaFetcherAvalonia
{
    // Simple class to define Windows platform options
    public class Win32PlatformOptions
    {
        // Whether to use Windows UI Composition for backdrop effects
        public bool UseWindowsUIComposition { get; set; }
        
        // Corner radius for backdrop composition
        public int CompositionBackdropCornerRadius { get; set; }
    }
}
