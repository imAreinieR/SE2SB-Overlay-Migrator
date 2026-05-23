using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Data
{
    public class Widget: INotifyPropertyChanged
    {
        private string _name;
        private string _folderLocation;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(DeployedLocation));
            }
        }

        public string FolderLocation
        {
            get => _folderLocation;
            set
            {
                if (_folderLocation == value)
                    return;

                _folderLocation = value;
                OnPropertyChanged(nameof(FolderLocation));
                OnPropertyChanged(nameof(DeployedLocation));
            }
        }

        public ObservableCollection<WidgetFile> Files { get; set; }

        public Widget(string name, string folderLocation)
        {
            _name           = name;
            _folderLocation = folderLocation;
            Files           = new ObservableCollection<WidgetFile>();
        }

        public Widget(int id, string name, string folderLocation, List<WidgetFile> files)
        {
            Id              = id;
            _name           = name;
            _folderLocation = folderLocation;
            Files           = new ObservableCollection<WidgetFile>(files);
        }

        public string DeployedLocation
            => Path.Combine(FolderLocation, Name.Replace(" ", "-"));

        public string HtmlFilePath
            => Path.Combine(DeployedLocation, "index.html");

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}