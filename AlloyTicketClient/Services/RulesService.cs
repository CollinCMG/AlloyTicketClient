using AlloyTicketClient.Contexts;
using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AlloyTicketClient.Services
{
    public class RulesService
    {
        private readonly AlloyTicketRulesDbContext _db;
        private readonly UserRoleService _userRoleService;
        private readonly IMemoryCache _cache;
        private static readonly string AllRulesCacheKey = "AlloyTicketRules_All";
        private static string FormRulesCacheKey(Guid formId) => $"AlloyTicketRules_{formId}";

        public RulesService(AlloyTicketRulesDbContext db, UserRoleService userRoleService, IMemoryCache cache)
        {
            _db = db;
            _userRoleService = userRoleService;
            _cache = cache;
        }

        public async Task<List<RuleConfig>> GetRulesForFormAsync(Guid formId)
        {
            var cacheKey = FormRulesCacheKey(formId);
            if (!_cache.TryGetValue(cacheKey, out List<RuleConfig> rules))
            {
                rules = await _db.AlloyTicketRules.Where(r => r.FormId == formId).ToListAsync();
                _cache.Set(cacheKey, rules, TimeSpan.FromMinutes(10));
            }
            return rules;
        }

        public async Task<List<RuleConfig>> GetAllRulesAsync()
        {
            if (!_cache.TryGetValue(AllRulesCacheKey, out List<RuleConfig> rules))
            {
                rules = await _db.AlloyTicketRules.ToListAsync();
                _cache.Set(AllRulesCacheKey, rules, TimeSpan.FromMinutes(10));
            }
            return rules ?? new List<RuleConfig>();
        }

        public async Task AddRuleAsync(RuleConfig rule)
        {
            if (rule.RuleId == Guid.Empty)
                rule.RuleId = Guid.NewGuid();
            _db.AlloyTicketRules.Add(rule);
            await _db.SaveChangesAsync();
            InvalidateRuleCache(rule.FormId);
        }

        public async Task UpdateRuleAsync(RuleConfig rule)
        {
            var existingRule = await _db.AlloyTicketRules.FirstOrDefaultAsync(r => r.RuleId == rule.RuleId);
            if (existingRule != null)
            {
                // Update properties
                existingRule.FormId = rule.FormId;

                existingRule.FormName = rule.FormName;
                existingRule.TriggerField = rule.TriggerField;
                existingRule.TriggerFieldLabel = rule.TriggerFieldLabel;
                existingRule.TriggerValue = rule.TriggerValue;
                existingRule.Action = rule.Action;
                existingRule.TargetList = rule.TargetList;
                existingRule.TargetFieldLabels = rule.TargetFieldLabels;
                existingRule.IsSet = rule.IsSet;
                existingRule.IsQueue = rule.IsQueue;
                existingRule.RoleName = rule.RoleName;
                // Add any other properties that need to be updated
                await _db.SaveChangesAsync();
                InvalidateRuleCache(rule.FormId);
            }
        }

        public async Task RemoveRuleAsync(Guid ruleId)
        {
            var rule = await _db.AlloyTicketRules.FirstOrDefaultAsync(r => r.RuleId == ruleId);
            if (rule != null)
            {
                _db.AlloyTicketRules.Remove(rule);
                await _db.SaveChangesAsync();
                InvalidateRuleCache(rule.FormId);
            }
        }

        public async Task<List<Guid>> GetAllFormIdsAsync()
        {
            return await _db.AlloyTicketRules.Select(r => r.FormId).Distinct().ToListAsync();
        }

        public async Task EvaluateRulesAsync(Guid formId, List<PageDto> pages, Dictionary<string, object?> fieldValues, string? changedField = null)
        {
            var rules = await GetRulesForFormAsync(formId);

            var hideRules = new List<RuleConfig>();
            var showRules = new List<RuleConfig>();
            foreach (var rule in rules)
            {
                switch (rule.Action)
                {
                    case FilterAction.Hide: hideRules.Add(rule); break;
                    case FilterAction.Show: showRules.Add(rule); break;
                }
            }

            // 1. Hide all target fields of Show rules by default
            foreach (var rule in showRules)
            {
                foreach (var target in rule.TargetList)
                {
                    foreach (var page in pages)
                    {
                        foreach (var item in page.Items)
                        {
                            if (item is FieldInputDto f && f.DefinitionID?.ToString() == target.FieldId)
                                f.IsHidden = true;
                        }
                    }
                }
            }

            // 2. Reset all other fields to not hidden (unless a Hide rule will apply)
            foreach (var page in pages)
            {
                foreach (var item in page.Items)
                {
                    if (item is FieldInputDto f)
                    {
                        // Only reset if not already hidden by Show rule default
                        if (!showRules.SelectMany(r => r.TargetList).Any(t => t.FieldId == f.DefinitionID?.ToString()))
                            f.IsHidden = false;
                    }
                }
            }

            // 3. Hide logic (set IsHidden = true for triggered Hide rules)
            foreach (var rule in hideRules)
            {
                if (fieldValues.TryGetValue(rule.TriggerField, out var value))
                {
                    bool isActive = false;
                    string? valueStr = value?.ToString();
                    bool triggerMatch = false;
                    if (value is DropdownOptionDto dto)
                    {
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                        valueStr = dto.ToString();
                        triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (value is List<string> multiList) // MultiSelect special case
                    {
                        isActive = multiList.Any();
                        triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || multiList.Any(v => string.Equals(v, rule.TriggerValue, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(valueStr);
                        triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    }
                    if (isActive && triggerMatch)
                    {
                        foreach (var target in rule.TargetList)
                        {
                            foreach (var page in pages)
                            {
                                foreach (var item in page.Items)
                                {
                                    if (item is FieldInputDto f && f.DefinitionID?.ToString() == target.FieldId)
                                        f.IsHidden = true;
                                }
                            }
                        }
                    }
                }
            }

            // 4. Show logic (set IsHidden = false for triggered Show rules)
            foreach (var rule in showRules)
            {
                if (fieldValues.TryGetValue(rule.TriggerField, out var value))
                {
                    bool isActive = false;
                    string? valueStr = value?.ToString();
                    bool triggerMatch = false;
                    if (value is DropdownOptionDto dto)
                    {
                        isActive = dto.Properties.Values.Any(v => v != null && !string.IsNullOrWhiteSpace(v.ToString()));
                        valueStr = dto.ToString();
                        triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (value is List<string> multiList) // MultiSelect special case
                    {
                        isActive = multiList.Any();
                        triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || multiList.Any(v => string.Equals(v, rule.TriggerValue, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        isActive = !string.IsNullOrWhiteSpace(valueStr);
                        triggerMatch = string.IsNullOrWhiteSpace(rule.TriggerValue) || string.Equals(valueStr, rule.TriggerValue, StringComparison.OrdinalIgnoreCase);
                    }
                    if (isActive && triggerMatch)
                    {
                        foreach (var target in rule.TargetList)
                        {
                            foreach (var page in pages)
                            {
                                foreach (var item in page.Items)
                                {
                                    if (item is FieldInputDto f && f.DefinitionID?.ToString() == target.FieldId)
                                        f.IsHidden = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        public async Task EvaluateModifyAppsRulesAsync(Guid formId, Dictionary<string, object?> fieldValues, string? changedField = null)
        {
            var rules = await GetRulesForFormAsync(formId);
            var modifyAppsRules = rules.Where(r => r.Action == FilterAction.ModifyApps).ToList();

            var modifiedApps = new Dictionary<string, string>();
            foreach (var rule in modifyAppsRules)
            {
                // Only trigger if changedField matches the rule's TriggerField
                if (changedField != null && rule.TriggerField != changedField)
                    continue;

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
                        valueStr = dto.ToString();
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
                        var roles = await _userRoleService.GetRolesForUserAsync(username);
                        var queues = await _userRoleService.GetUserQueuesAsync(username);
                        var apps = roles.Select(x => x.AppCode).Distinct();

                        foreach (var target in rule.TargetList)
                        {
                            // Use TargetValueOverride if present for ModifyApps
                            string? overrideValue = null;
                            if (!string.IsNullOrWhiteSpace(rule.TargetValueOverride))
                            {
                                overrideValue = rule.TargetValueOverride;
                            }

                            var targetValue = apps.Contains(target.FieldName) ? overrideValue ?? target.FieldValue : target.FieldValue;
                            modifiedApps[target.FieldId] = targetValue;

                            var fieldByRoleRules = rules.Where(r => r.Action == FilterAction.FieldsByRole && r.TriggerField.Equals(target.FieldId) && (string.IsNullOrWhiteSpace(r.TriggerValue) || r.TriggerValue.Equals(targetValue))).ToList();

                            foreach (var roleRule in fieldByRoleRules)
                            {
                                var targetRoleRule = roleRule.TargetList.FirstOrDefault();
                                if (targetRoleRule == null)
                                    continue;

                                // Parse TargetValueOverride as comma-separated values
                                var targetValueOverrides = (roleRule.TargetValueOverride ?? string.Empty)
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .ToArray();
                                var positiveValue = targetValueOverrides.Length > 0 ? targetValueOverrides[0] : string.Empty;
                                var negativeValue = targetValueOverrides.Length > 1 ? targetValueOverrides[1] : string.Empty;
                                string? targetRuleValue = null;

                                bool isRoleMatch = false;
                                if (roleRule.RoleName == null)
                                {
                                    isRoleMatch = true;
                                }
                                else
                                {
                                    var ruleRoles = roles.Where(x => x.AppCode == target.FieldName).Select(r => r.RoleName).Distinct().ToList();
                                    isRoleMatch = ruleRoles.Count > 0 && ruleRoles.Contains(roleRule.RoleName.ToString());
                                }

                                if (isRoleMatch)
                                {
                                    // Positive case
                                    if (!string.IsNullOrWhiteSpace(positiveValue))
                                    {
                                        targetRuleValue = positiveValue;
                                    }
                                    else if (roleRule.IsQueue)
                                    {
                                        targetRuleValue = queues.FirstOrDefault(q => q.Key.Equals(target.FieldName)).Value;
                                    }
                                    modifiedApps[targetRoleRule.FieldId] = targetRuleValue ?? string.Empty;
                                }
                                else
                                {
                                    // Negative case
                                    if (!string.IsNullOrWhiteSpace(negativeValue))
                                    {
                                        targetRuleValue = negativeValue;
                                    }
                                    else
                                    {
                                        targetRuleValue = string.Empty;
                                    }
                                    modifiedApps[targetRoleRule.FieldId] = targetRuleValue;
                                }
                            }
                        }
                    }
                }
            }

            if (modifiedApps?.Count > 0)
            {
                foreach (var kvp in modifiedApps)
                    fieldValues[kvp.Key] = kvp.Value;
            }
        }

        private void InvalidateRuleCache(Guid formId)
        {
            _cache.Remove(FormRulesCacheKey(formId));
            _cache.Remove(AllRulesCacheKey);
        }
    }

    public class RuleEvaluationResult
    {
        public Dictionary<string, string> ModifiedApps { get; set; } = new();
    }
}