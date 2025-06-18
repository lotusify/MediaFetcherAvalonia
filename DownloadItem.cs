using System.ComponentModel;

namespace MediaFetcherAvalonia
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private double _progress;
        public string Url { get; set; } = string.Empty;
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
