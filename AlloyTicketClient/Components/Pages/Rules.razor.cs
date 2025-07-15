using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using AlloyTicketClient.Models;
using AlloyTicketClient.Services;
using AlloyTicketClient.Enums;

namespace AlloyTicketClient.Components.Pages
{
    public partial class Rules : ComponentBase
    {
        // Represents a form for display in the rules UI
        private class FormInfo
        {
            public string FormId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private FormFieldService FormFieldService { get; set; } = default!;
        [Inject] private RulesService RulesService { get; set; } = default!;

        private List<FormInfo> Forms { get; set; } = new();
        private List<RuleConfig> RulesList { get; set; } = new();
        private List<FieldInputDto> FormFields { get; set; } = new();
        private string? SelectedFormId, SelectedFieldId;
        private FilterAction SelectedAction { get; set; } = FilterAction.Hide;
        private List<string> SelectedTargetFieldIds { get; set; } = new();

        // Modal state
        private bool ShowRuleModal, IsEditMode;
        private RuleConfig? EditingRule;
        private string? ModalSelectedFormId, ModalSelectedFieldId, ModalTriggerValue;
        private FilterAction ModalSelectedAction { get; set; } = FilterAction.Hide;
        private List<string> ModalSelectedTargetFieldIds { get; set; } = new();
        private List<FieldInputDto> ModalFormFields { get; set; } = new();

        private async Task OpenRuleModal(RuleConfig? rule)
        {
            ShowRuleModal = true;
            IsEditMode = rule != null;
            if (rule == null)
            {
                EditingRule = null;
                ModalSelectedFormId = null;
                ModalSelectedFieldId = null;
                ModalSelectedAction = FilterAction.Hide;
                ModalSelectedTargetFieldIds.Clear();
                ModalFormFields.Clear();
                ModalTriggerValue = null;
            }
            else
            {
                EditingRule = RulesList.FirstOrDefault(r => r.RuleId == rule.RuleId);
                ModalSelectedFormId = EditingRule?.FormId;
                ModalSelectedFieldId = EditingRule?.TriggerField;
                ModalSelectedAction = EditingRule?.Action ?? FilterAction.Hide;
                ModalSelectedTargetFieldIds = EditingRule?.TargetList.Select(t => t.FieldId).ToList() ?? new();
                ModalTriggerValue = EditingRule?.TriggerValue;
                if (!string.IsNullOrEmpty(ModalSelectedFormId) && Guid.TryParse(ModalSelectedFormId, out var formGuid))
                {
                    var pages = await FormFieldService.GetFormPagesAsync(formGuid);
                    ModalFormFields = pages.SelectMany(p => p.Items).OfType<FieldInputDto>().ToList();
                }
            }
        }

        private void CloseRuleModal()
        {
            ShowRuleModal = false;
            EditingRule = null;
        }

        private async Task OnModalFormSelected(ChangeEventArgs e)
        {
            ModalSelectedFormId = e.Value?.ToString();
            ModalSelectedFieldId = null;
            ModalSelectedTargetFieldIds.Clear();
            ModalFormFields.Clear();
            if (!string.IsNullOrEmpty(ModalSelectedFormId) && Guid.TryParse(ModalSelectedFormId, out var formGuid))
            {
                var pages = await FormFieldService.GetFormPagesAsync(formGuid);
                ModalFormFields = pages.SelectMany(p => p.Items).OfType<FieldInputDto>().ToList();
            }
            StateHasChanged();
        }

        private void OnModalTargetFieldsChanged(ChangeEventArgs e)
        {
            if (e.Value is string single)
                ModalSelectedTargetFieldIds = new() { single };
            else if (e.Value is IEnumerable<string> selectedOptions)
                ModalSelectedTargetFieldIds = selectedOptions.ToList();
            else
                ModalSelectedTargetFieldIds = new();
        }

        private bool CanAddRuleFromModal =>
            !string.IsNullOrEmpty(ModalSelectedFormId) &&
            !string.IsNullOrEmpty(ModalSelectedFieldId) &&
            ModalSelectedTargetFieldIds.Any();

        private async Task SaveRuleFromModal()
        {
            if (!CanAddRuleFromModal) return;
            var triggerFieldLabel = GetModalFieldLabelById(ModalSelectedFieldId);
            var targetFieldLabels = ModalSelectedTargetFieldIds.Select(GetModalFieldLabelById).ToList();
            var formName = Forms.FirstOrDefault(f => f.FormId == ModalSelectedFormId)?.Name ?? ModalSelectedFormId;
            var targetList = ModalSelectedTargetFieldIds
                .Select(id =>
                {
                    var field = ModalFormFields.FirstOrDefault(f => f.DefinitionID?.ToString() == id);
                    return new TargetFieldInfo
                    {
                        FieldId = id,
                        FieldName = field?.FieldName ?? id,
                        FieldType = field?.FieldType ?? FieldType.Null
                    };
                })
                .ToList();
            if (IsEditMode && EditingRule != null)
            {
                EditingRule.FormId = ModalSelectedFormId;
                EditingRule.FormName = formName;
                EditingRule.TriggerField = ModalSelectedFieldId;
                EditingRule.TriggerFieldLabel = triggerFieldLabel;
                EditingRule.TriggerValue = string.IsNullOrWhiteSpace(ModalTriggerValue) ? null : ModalTriggerValue;
                EditingRule.Action = ModalSelectedAction;
                EditingRule.TargetList = targetList;
                EditingRule.TargetFieldLabels = targetFieldLabels;
                await RulesService.UpdateRuleAsync(EditingRule);
            }
            else
            {
                var rule = new RuleConfig
                {
                    RuleId = Guid.NewGuid(),
                    FormId = ModalSelectedFormId,
                    FormName = formName,
                    TriggerField = ModalSelectedFieldId,
                    TriggerFieldLabel = triggerFieldLabel,
                    TriggerValue = string.IsNullOrWhiteSpace(ModalTriggerValue) ? null : ModalTriggerValue,
                    Action = ModalSelectedAction,
                    TargetList = targetList,
                    TargetFieldLabels = targetFieldLabels
                };
                await RulesService.AddRuleAsync(rule);
            }
            RulesList = await RulesService.GetAllRulesAsync();
            CloseRuleModal();
            StateHasChanged();
        }

        private string GetModalFieldLabelById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return id ?? string.Empty;
            var field = ModalFormFields.FirstOrDefault(f => f.DefinitionID?.ToString() == id);
            return !string.IsNullOrWhiteSpace(field?.FieldLabel) ? field.FieldLabel : (field?.FieldName ?? id);
        }

        private void OnTargetFieldsChanged(ChangeEventArgs e)
        {
            if (e.Value is string single)
                SelectedTargetFieldIds = new() { single };
            else if (e.Value is IEnumerable<string> selectedOptions)
                SelectedTargetFieldIds = selectedOptions.ToList();
            else
                SelectedTargetFieldIds = new();
        }

        private void AddRule()
        {
            if (!string.IsNullOrEmpty(SelectedFormId) && !string.IsNullOrEmpty(SelectedFieldId) && SelectedTargetFieldIds.Any())
            {
                var triggerFieldLabel = GetFieldLabelById(SelectedFieldId);
                var targetFieldLabels = SelectedTargetFieldIds.Select(GetFieldLabelById).ToList();
                var formName = Forms.FirstOrDefault(f => f.FormId == SelectedFormId)?.Name ?? SelectedFormId;
                var targetList = SelectedTargetFieldIds
                    .Select(id =>
                    {
                        var field = FormFields.FirstOrDefault(f => f.DefinitionID?.ToString() == id);
                        return new TargetFieldInfo
                        {
                            FieldId = id,
                            FieldName = field?.FieldName ?? id,
                            FieldType = field?.FieldType ?? FieldType.Null
                        };
                    })
                    .ToList();
                var rule = new RuleConfig
                {
                    FormId = SelectedFormId,
                    FormName = formName,
                    TriggerField = SelectedFieldId,
                    TriggerFieldLabel = triggerFieldLabel,
                    Action = SelectedAction,
                    TargetList = targetList,
                    TargetFieldLabels = targetFieldLabels
                };
                RulesList.Add(rule);
                RulesConfig.Instance.Rules.Add(rule);
                SelectedFieldId = null;
                SelectedAction = FilterAction.Hide;
                SelectedTargetFieldIds.Clear();
                StateHasChanged();
            }
        }

        private async void DeleteRule(RuleConfig rule)
        {
            if (rule.RuleId != default)
            {
                RulesList.RemoveAll(r => r.RuleId == rule.RuleId);
                RulesConfig.Instance.Rules.RemoveAll(r => r.RuleId == rule.RuleId);
                await RulesService.RemoveRuleAsync(rule.RuleId);
            }
            else
            {
                RulesList.RemoveAll(r => r.FormId == rule.FormId && r.TriggerField == rule.TriggerField && r.Action == rule.Action);
                RulesConfig.Instance.Rules.RemoveAll(r => r.FormId == rule.FormId && r.TriggerField == rule.TriggerField && r.Action == rule.Action);
            }
            StateHasChanged();
        }

        private string GetFieldLabelById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return id ?? string.Empty;
            var field = FormFields.FirstOrDefault(f => f.DefinitionID?.ToString() == id);
            return !string.IsNullOrWhiteSpace(field?.FieldLabel) ? field.FieldLabel : (field?.FieldName ?? id);
        }

        protected override async Task OnInitializedAsync()
        {
            var dynamicPages = Configuration.GetSection("DynamicPages").Get<List<DynamicPageConfig>>() ?? new();
            foreach (var page in dynamicPages)
            {
                if (page.Buttons != null)
                {
                    foreach (var btn in page.Buttons)
                    {
                        Forms.Add(new FormInfo { FormId = btn.FormId, Name = btn.Name });
                    }
                }
            }
            RulesList = await RulesService.GetAllRulesAsync();
        }

        private async Task OnFormSelected(ChangeEventArgs e)
        {
            SelectedFormId = e.Value?.ToString();
            SelectedFieldId = null;
            SelectedAction = FilterAction.Hide;
            SelectedTargetFieldIds.Clear();
            FormFields.Clear();
            if (!string.IsNullOrEmpty(SelectedFormId) && Guid.TryParse(SelectedFormId, out var formGuid))
            {
                var pages = await FormFieldService.GetFormPagesAsync(formGuid);
                FormFields = pages.SelectMany(p => p.Items).OfType<FieldInputDto>().ToList();
            }
            StateHasChanged();
        }
    }
}
