namespace StreamElementsToStreamerBotMigrationTool.Data
{
    public class Widget
    {
        public int    Id             { get; set; }
        public string Name           { get; set; }
        public string Description    { get; set; }
        public string FolderLocation { get; set; }

        public Widget(string name, string description, string folderLocation)
        {
            Name           = name;
            Description    = description;
            FolderLocation = folderLocation;
        }

        public Widget(int id, string name, string description, string folderLocation)
        {
            Id             = id;
            Name           = name;
            Description    = description;
            FolderLocation = folderLocation;
        }
    }
}