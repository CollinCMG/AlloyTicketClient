namespace AlloyTicketClient.Models
{
    public class DynamicButtonConfig
    {
        public Guid FormId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string ObjectId { get; set; } = string.Empty;
    }

    public class DynamicPageConfig
    {
        public string HeaderText { get; set; } = string.Empty;
        public string PageText { get; set; } = string.Empty;
        public string SubText { get; set; } = string.Empty;
        public List<DynamicButtonConfig> Buttons { get; set; } = new();
    }

    public class AttachmentConfig
    {
        public string Caption { get; set; } = string.Empty;
        public bool ForProgram { get; set; }
        public bool Mandatory { get; set; }
        public bool ReadOnly { get; set; }
        public bool InclFiles { get; set; }
        public bool InclExisting { get; set; }
    }
}
