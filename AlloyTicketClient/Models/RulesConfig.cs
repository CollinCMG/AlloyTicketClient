using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Models
{
    public class TargetFieldInfo
    {
        public string FieldId { get; set; } = string.Empty; // GUID as string
        public string FieldName { get; set; } = string.Empty;
        public FieldType FieldType { get; set; } = FieldType.Null; // Use enum
    }

    public class RuleConfig
    {
        public string FormId { get; set; } = string.Empty;
        public string? FormName { get; set; } // Display name for the form
        public string TriggerField { get; set; } = string.Empty;
        public string? TriggerFieldLabel { get; set; } // Display name for trigger field
        public bool IsSet { get; set; } = true;
        public string Action { get; set; } = "hide";
        public List<TargetFieldInfo> TargetList { get; set; } = new(); // List of target fields (name + guid)
        public List<string>? TargetFieldLabels { get; set; } // Display names for target fields
    }

    public class RulesConfig
    {
        private static RulesConfig? _instance;
        public static RulesConfig Instance => _instance ??= new RulesConfig();
        public List<RuleConfig> Rules { get; set; } = new();
    }
}
