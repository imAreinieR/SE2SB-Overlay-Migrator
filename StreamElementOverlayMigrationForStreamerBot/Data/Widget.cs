namespace StreamElementsToStreamerBotMigrationTool.Data
{
    public class Widget
    {
        public int    Id             { get; set; }
        public string Name           { get; set; }
        public string FolderLocation { get; set; }

        public Widget(string name, string folderLocation)
        {
            Name           = name;
            FolderLocation = folderLocation;
        }

        public Widget(int id, string name, string folderLocation)
        {
            Id             = id;
            Name           = name;
            FolderLocation = folderLocation;
        }
    }
}