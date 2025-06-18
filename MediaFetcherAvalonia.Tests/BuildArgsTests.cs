using MediaFetcherAvalonia;
using Xunit;

namespace MediaFetcherAvalonia.Tests
{
    public class BuildArgsTests
    {
        [Fact]
        public void IncludesSponsorBlockCategories()
        {
            var settings = new AppSettings { SponsorBlockCategories = "sponsor,intro" };
            var window = new MainWindow(settings);
            var args = typeof(MainWindow).GetMethod("BuildArgs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(window, new object[] { "https://example.com" }) as string;
            Assert.Contains("--sponsorblock-remove sponsor,intro", args);
        }
    }
}
