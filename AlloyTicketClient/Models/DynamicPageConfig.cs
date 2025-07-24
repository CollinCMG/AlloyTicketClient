namespace AlloyTicketClient.Models
{
    public class DynamicButtonConfig
    {
        public Guid FormId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ObjectId { get; set; } = string.Empty;
        public int? ActionId { get; set; }
        public RequestType Type { get; set; }
    }

    public class DynamicPageConfig
    {
        public string HeaderText { get; set; } = string.Empty;
        public string PageText { get; set; } = string.Empty;
        public string SubText { get; set; } = string.Empty;
        public List<DynamicButtonConfig> Buttons { get; set; } = new();
    }
}