namespace MediaFetcherAvalonia
{
    // Resolution data class for UI display and value mapping
    public class Resolution
    {
        public string Display { get; }
        public string Value { get; }
        
        public Resolution(string display, string value)
        {
            Display = display;
            Value = value;
        }
        
        public override string ToString()
        {
            return Display;
        }
    }
}
