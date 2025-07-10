using AlloyTicketClient.Models;
using System.Threading.Tasks;

namespace AlloyTicketClient.Services
{
    public class RulesService
    {
        private readonly Dictionary<string, List<RuleConfig>> _rulesByFormId = new();
        private readonly UserRoleService _userRoleService;

        public RulesService(UserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        public Task<List<RuleConfig>> GetRulesForFormAsync(string formId)
        {
            _rulesByFormId.TryGetValue(formId, out var rules);
            return Task.FromResult(rules ?? new List<RuleConfig>());
        }

        public Task AddRuleAsync(string formId, RuleConfig rule)
        {
            if (!_rulesByFormId.ContainsKey(formId))
                _rulesByFormId[formId] = new List<RuleConfig>();
            _rulesByFormId[formId].Add(rule);
            return Task.CompletedTask;
        }

        public Task RemoveRuleAsync(string formId, RuleConfig rule)
        {
            if (_rulesByFormId.ContainsKey(formId))
                _rulesByFormId[formId].Remove(rule);
            return Task.CompletedTask;
        }

        public Task<List<string>> GetAllFormIdsAsync()
        {
            return Task.FromResult(_rulesByFormId.Keys.ToList());
        }

        // Unified rule evaluation for Show, Hide, Modify Apps
        public async Task<RuleEvaluationResult> EvaluateRulesAsync(string formId, Dictionary<string, object?> fieldValues, string? changedField = null)
        {
            var toHide = new HashSet<string>();
            var toShow = new HashSet<string>();
            var allShowTargets = new HashSet<string>();
            var modifiedApps = new Dictionary<string, string>();
            var results = await _userRoleService.GetRolesForUserAsync("cbuus");

            var rules = _rulesByFormId.TryGetValue(formId, out var localRules)
                ? localRules
                : RulesConfig.Instance.Rules.Where(r => r.FormId == formId).ToList();

            // Always evaluate all hide/show rules
            foreach (var rule in rules)
            {
                if (rule.Action == "hide")
                {
                    // Use field id (GUID) as key
                    if (fieldValues.TryGetValue(rule.TriggerField, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                        foreach (var target in rule.List)
                            toHide.Add(target);
                }
                else if (rule.Action == "show")
                {
                    foreach (var target in rule.List)
                        allShowTargets.Add(target);
                    // Use field id (GUID) as key
                    if (fieldValues.TryGetValue(rule.TriggerField, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                        foreach (var target in rule.List)
                            toShow.Add(target);
                }
            }

            // Only evaluate modifyapps rules for the changed field
            if (!string.IsNullOrEmpty(changedField))
            {
                var modifyRules = rules.Where(r => r.Action == "modifyapps" && r.TriggerField == changedField).ToList();
                foreach (var rule in modifyRules)
                {
                    // Use field id (GUID) as key
                    if (fieldValues.TryGetValue(rule.TriggerField, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                    {
                        foreach (var target in rule.List)
                        {
                            modifiedApps[target] = value.ToString();
                        }
                    }
                }
            }

            // For show rules: hide all show-targets unless their trigger is active
            foreach (var target in allShowTargets)
                if (!toShow.Contains(target))
                    toHide.Add(target);

            return new RuleEvaluationResult
            {
                FieldsToHide = toHide.ToList(),
                ModifiedApps = modifiedApps
            };
        }
    }

    public class RuleEvaluationResult
    {
        public List<string> FieldsToHide { get; set; } = new();
        public Dictionary<string, string> ModifiedApps { get; set; } = new();
    }
}
