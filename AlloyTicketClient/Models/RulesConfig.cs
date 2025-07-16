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
        public Guid FormId { get; set; }
        public string ObjectId { get; set; } = string.Empty;
        public Guid RuleId { get; set; }
        public string? FormName { get; set; } // Display name for the form
        public string TriggerField { get; set; } = string.Empty;
        public string? TriggerFieldLabel { get; set; } // Display name for trigger field
        public string? TriggerValue { get; set; } // Value that triggers the rule, null or special for any value
        public bool IsSet { get; set; } = true;
        public FilterAction Action { get; set; } = FilterAction.Hide;
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
