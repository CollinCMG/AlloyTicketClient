namespace AlloyTicketClient.Models
{
    public class RuleConfig
    {
        public string FormId { get; set; } = string.Empty;
        public string? FormName { get; set; } // Display name for the form
        public string TriggerField { get; set; } = string.Empty;
        public string? TriggerFieldLabel { get; set; } // Display name for trigger field
        public bool IsSet { get; set; } = true;
        public string Action { get; set; } = "hide";
        public List<string> List { get; set; } = new();
        public List<string>? TargetFieldLabels { get; set; } // Display names for target fields
    }

    public class RulesConfig
    {
        private static RulesConfig? _instance;
        public static RulesConfig Instance => _instance ??= new RulesConfig();
        public List<RuleConfig> Rules { get; set; } = new();
    }
}
