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
            // Prepare result containers
            var fieldsToHide = new HashSet<string>();
            var fieldsToShow = new HashSet<string>();
            var showTargets = new HashSet<string>();
            var modifiedApps = new Dictionary<string, string>();

            // Get rules for the form
            var rules = _rulesByFormId.TryGetValue(formId, out var localRules)
                ? localRules
                : RulesConfig.Instance.Rules.Where(r => r.FormId == formId).ToList();

            // Partition rules by action for clarity
            var hideRules = new List<RuleConfig>();
            var showRules = new List<RuleConfig>();
            var modifyAppsRules = new List<RuleConfig>();
            foreach (var rule in rules)
            {
                switch (rule.Action)
                {
                    case "hide": hideRules.Add(rule); break;
                    case "show": showRules.Add(rule); break;
                    case "modifyapps": modifyAppsRules.Add(rule); break;
                }
            }

            // Evaluate hide rules
            foreach (var rule in hideRules)
            {
                if (fieldValues.TryGetValue(rule.TriggerField, out var value))
                {
                    bool isActive = false;
                    if (value is DropdownOptionDto dto)
                    {
                        // Consider active if any property is non-null/non-empty
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(value?.ToString());
                    }
                    if (isActive)
                    {
                        foreach (var target in rule.TargetList)
                            fieldsToHide.Add(target.FieldId);
                    }
                }
            }

            // Evaluate show rules
            foreach (var rule in showRules)
            {
                foreach (var target in rule.TargetList)
                    showTargets.Add(target.FieldId);
                if (fieldValues.TryGetValue(rule.TriggerField, out var value))
                {
                    bool isActive = false;
                    if (value is DropdownOptionDto dto)
                    {
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(value?.ToString());
                    }
                    if (isActive)
                    {
                        foreach (var target in rule.TargetList)
                            fieldsToShow.Add(target.FieldId);
                    }
                }
            }

            // Hide all show-targets unless their trigger is active
            foreach (var target in showTargets)
            {
                if (!fieldsToShow.Contains(target))
                    fieldsToHide.Add(target);
            }

            // Evaluate modifyapps rules with app name/field name mapping
            foreach (var rule in modifyAppsRules)
            {
                if (fieldValues.TryGetValue(rule.TriggerField, out var value))
                {
                    bool isActive = false;
                    DropdownOptionDto? triggerDto = null;
                    string username = string.Empty;
                    if (value is DropdownOptionDto dto)
                    {
                        triggerDto = dto;
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                        if (dto.Properties.TryGetValue("Primary_Email", out var emailObj) && emailObj is string email && !string.IsNullOrWhiteSpace(email))
                        {
                            var resolvedUsername = await _userRoleService.GetUsernameByEmailAsync(email);
                            if (!string.IsNullOrWhiteSpace(resolvedUsername))
                            {
                                username = resolvedUsername;
                            }
                        }
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(value?.ToString());
                    }
                    if (isActive)
                    {
                        var result = await _userRoleService.GetRolesForUserAsync(username);
                        var apps = result.Select(x => x.AppCode).Distinct();
                        // Only set value if app name matches field name
                        foreach (var target in rule.TargetList)
                        {
                            var targetValue = target.FieldType == Enums.FieldType.Checkbox
                                ? (apps.Contains(target.FieldName) ? "True" : "False")
                                : (apps.Contains(target.FieldName) ? "Yes" : "No");
                            modifiedApps[target.FieldId] = targetValue;
                        }
                        // Example: you can now use triggerDto.Properties["Id"] or ["Display_Name"] for more advanced logic
                    }
                }
            }

            return new RuleEvaluationResult
            {
                FieldsToHide = fieldsToHide.ToList(),
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
