using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Models
{
    public class TargetFieldInfo
    {
        public string FieldId { get; set; } = string.Empty; // GUID as string
        public string FieldName { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public FieldType FieldType { get; set; } = FieldType.Null; // Use enum
    }
}
