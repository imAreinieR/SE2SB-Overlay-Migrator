using System.IO;

namespace StreamElementsToStreamerBotMigrationTool.Data
{
    public class Widget
    {
        public int              Id             { get; set; }
        public string           Name           { get; set; }
        public string           FolderLocation { get; set; }
        public List<WidgetFile> Files          { get; set; }

        public Widget(string name, string folderLocation)
        {
            Name           = name;
            FolderLocation = folderLocation;
            Files          = new List<WidgetFile>();
        }

        public Widget(int id, string name, string folderLocation, List<WidgetFile> files)
        {
            Id             = id;
            Name           = name;
            FolderLocation = folderLocation;
            Files          = files;
        }

        public string DeployedLocation
            => Path.Combine(FolderLocation, Name.Replace(" ", "-"));
    }
}