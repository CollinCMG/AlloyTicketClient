namespace AlloyTicketClient.Models
{
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
