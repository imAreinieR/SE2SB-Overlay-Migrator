using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Data
{
    public class Widget: INotifyPropertyChanged
    {
        private string _name;
        private string _rootFolderLocation;

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
                OnPropertyChanged(nameof(FolderLocation));
                OnPropertyChanged(nameof(HtmlFilePath));
                OnPropertyChanged(nameof(IsGenerated));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public string RootFolderLocation
        {
            get => _rootFolderLocation;
            set
            {
                if (_rootFolderLocation == value)
                    return;

                _rootFolderLocation = value;
                OnPropertyChanged(nameof(RootFolderLocation));
                OnPropertyChanged(nameof(FolderLocation));
                OnPropertyChanged(nameof(HtmlFilePath));
                OnPropertyChanged(nameof(IsGenerated));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public ObservableCollection<WidgetFile> Files { get; set; }

        public Widget(string name, string folderLocation)
        {
            _name               = name;
            _rootFolderLocation = folderLocation;
            Files               = new ObservableCollection<WidgetFile>();
        }

        public Widget(int id, string name, string folderLocation, List<WidgetFile> files)
        {
            Id                  = id;
            _name               = name;
            _rootFolderLocation = folderLocation;
            Files               = new ObservableCollection<WidgetFile>(files);
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string FolderLocation
            => Path.Combine(RootFolderLocation, Name.Replace(" ", "-"));

        public string HtmlFilePath
            => Path.Combine(FolderLocation, "index.html");

        public bool HasValidFileSet
            => Files.Any() && WidgetFileImportAndExportService.CheckIsValidFileSet(Files, out _);

        public bool IsGenerated
            => File.Exists(HtmlFilePath);

        public System.Windows.Media.SolidColorBrush StatusColor
            => new System.Windows.Media.SolidColorBrush
            (
                HasValidFileSet
                    ? IsGenerated
                        ? System.Windows.Media.Color.FromArgb(255, 60, 179, 113)
                        : System.Windows.Media.Color.FromArgb(255, 245, 166, 35)
                    : System.Windows.Media.Color.FromArgb(255, 220, 20, 60)
            );

        public void AddWidgetFile(WidgetFile widgetFile)
            => Files.AddSorted(widgetFile, WidgetFileComparer.Instance);

        public void NotifyStatusChanges()
        {
            OnPropertyChanged(nameof(FolderLocation));
            OnPropertyChanged(nameof(HtmlFilePath));
            OnPropertyChanged(nameof(HasValidFileSet));
            OnPropertyChanged(nameof(IsGenerated));
            OnPropertyChanged(nameof(StatusColor));
        }
    }
}