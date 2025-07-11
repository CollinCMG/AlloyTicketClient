using AlloyTicketClient.Models;
using AlloyTicketClient.Enums;

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

        public async Task<RuleEvaluationResult> EvaluateRulesAsync(string formId, Dictionary<string, object?> fieldValues, string? changedField = null)
        {
            var fieldsToHide = new HashSet<string>();
            var fieldsToShow = new HashSet<string>();
            var showTargets = new HashSet<string>();
            var modifiedApps = new Dictionary<string, string>();

            var rules = _rulesByFormId.TryGetValue(formId, out var localRules)
                ? localRules
                : RulesConfig.Instance.Rules.Where(r => r.FormId == formId).ToList();

            var hideRules = new List<RuleConfig>();
            var showRules = new List<RuleConfig>();
            var modifyAppsRules = new List<RuleConfig>();
            foreach (var rule in rules)
            {
                switch (rule.Action)
                {
                    case FilterAction.Hide: hideRules.Add(rule); break;
                    case FilterAction.Show: showRules.Add(rule); break;
                    case FilterAction.ModifyApps: modifyAppsRules.Add(rule); break;
                }
            }

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

            foreach (var target in showTargets)
            {
                if (!fieldsToShow.Contains(target))
                    fieldsToHide.Add(target);
            }

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
                        foreach (var target in rule.TargetList)
                        {
                            var targetValue = target.FieldType == Enums.FieldType.Checkbox
                                ? (apps.Contains(target.FieldName) ? "True" : "False")
                                : (apps.Contains(target.FieldName) ? "Yes" : "No");
                            modifiedApps[target.FieldId] = targetValue;
                        }
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
