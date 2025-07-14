using AlloyTicketClient.Contexts;
using AlloyTicketClient.Models;
using AlloyTicketClient.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AlloyTicketClient.Services
{
    public class RulesService
    {
        private readonly AlloyTicketRulesDbContext _db;
        private readonly UserRoleService _userRoleService;

        public RulesService(AlloyTicketRulesDbContext db, UserRoleService userRoleService)
        {
            _db = db;
            _userRoleService = userRoleService;
        }

        public async Task<List<RuleConfig>> GetRulesForFormAsync(string formId)
        {
            var rules = await _db.AlloyTicketRules.Where(r => r.FormId == formId).ToListAsync();
            return rules;
        }

        public async Task<List<RuleConfig>> GetAllRulesAsync()
        {
            var rules = await _db.AlloyTicketRules.ToListAsync();
            return rules;
        }

        public async Task AddRuleAsync(RuleConfig rule)
        {
            if (rule.RuleId == Guid.Empty)
                rule.RuleId = Guid.NewGuid();
            _db.AlloyTicketRules.Add(rule);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateRuleAsync(RuleConfig rule)
        {
            _db.AlloyTicketRules.Update(rule);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveRuleAsync(Guid ruleId)
        {
            var rule = await _db.AlloyTicketRules.FirstOrDefaultAsync(r => r.RuleId == ruleId);
            if (rule != null)
            {
                _db.AlloyTicketRules.Remove(rule);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetAllFormIdsAsync()
        {
            return await _db.AlloyTicketRules.Select(r => r.FormId).Distinct().ToListAsync();
        }

        public async Task<RuleEvaluationResult> EvaluateRulesAsync(string formId, Dictionary<string, object?> fieldValues, string? changedField = null)
        {
            var fieldsToHide = new HashSet<string>();
            var fieldsToShow = new HashSet<string>();
            var showTargets = new HashSet<string>();
            var modifiedApps = new Dictionary<string, string>();

            var rules = _db.AlloyTicketRules.Where(r => r.FormId == formId).ToList();

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
                    string? valueStr = value?.ToString();
                    if (value is DropdownOptionDto dto)
                    {
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                        valueStr = dto.ToString(); // You may want to adjust this if DropdownOptionDto has a specific value property
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(valueStr);
                    }
                    // Check trigger value logic
                    bool triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    if (isActive && triggerMatch)
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
                    string? valueStr = value?.ToString();
                    if (value is DropdownOptionDto dto)
                    {
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                        valueStr = dto.ToString(); // You may want to adjust this if DropdownOptionDto has a specific value property
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(valueStr);
                    }
                    bool triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    if (isActive && triggerMatch)
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
                    string? valueStr = value?.ToString();
                    if (value is DropdownOptionDto dto)
                    {
                        triggerDto = dto;
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                        valueStr = dto.ToString(); // You may want to adjust this if DropdownOptionDto has a specific value property
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
                        isActive = !string.IsNullOrWhiteSpace(valueStr);
                    }
                    bool triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    if (isActive && triggerMatch)
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
