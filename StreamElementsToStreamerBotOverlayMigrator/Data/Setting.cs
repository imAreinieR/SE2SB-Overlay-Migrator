namespace StreamElementsToStreamerBotOverlayMigrator.Data
{
    public class Setting
    {
        public int    Id    { get; set; } = 0;
        public string Name  { get; set; } = string.Empty;
        public string Type  { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public Setting(string name, string type, string value)
        {
            Name  = name;
            Type  = type;
            Value = value;
        }

        public Setting(int id, string name, string type, string value)
        {
            Id    = id;
            Name  = name;
            Type  = type;
            Value = value;
        }
    }
}