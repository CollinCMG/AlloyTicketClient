using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Components;

namespace AlloyTicketClient.Components.Pages
{
    public partial class Rules : ComponentBase
    {
        // Represents a form for display in the rules UI
        private class FormInfo
        {
            public Guid FormId { get; set; }
            public string ObjectId { get; set; }

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
        private Guid ModalSelectedFormId;
        private string? ModalSelectedFieldId, ModalTriggerValue;

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
                ModalSelectedFieldId = null;

                ModalSelectedAction = FilterAction.Hide;
                ModalSelectedTargetFieldIds.Clear();
                ModalFormFields.Clear();
                ModalTriggerValue = null;
            }
            else
            {
                EditingRule = RulesList.FirstOrDefault(r => r.RuleId == rule.RuleId);
                ModalSelectedFormId = EditingRule?.FormId ?? Guid.Empty;
                ModalSelectedFieldId = EditingRule?.TriggerField;

                ModalSelectedAction = EditingRule?.Action ?? FilterAction.Hide;
                ModalSelectedTargetFieldIds = EditingRule?.TargetList.Select(t => t.FieldId).ToList() ?? new();
                ModalTriggerValue = EditingRule?.TriggerValue;
                if (ModalSelectedFormId != Guid.Empty)
                {
                    var pages = await FormFieldService.GetFormPagesAsync(ModalSelectedFormId);
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
            if (e.Value == null || string.IsNullOrEmpty(e.Value.ToString()) 
                                || !Guid.TryParse(e.Value.ToString(), out var formId))
            {
                ModalSelectedFormId = Guid.Empty;
                ModalSelectedFieldId = null;
                ModalSelectedTargetFieldIds.Clear();
                ModalFormFields.Clear();
                return;
            }

            ModalSelectedFormId = formId;

            ModalSelectedTargetFieldIds.Clear();
            ModalFormFields.Clear();
            if (ModalSelectedFormId != Guid.Empty)
            {
                var pages = await FormFieldService.GetFormPagesAsync(ModalSelectedFormId);
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
            ModalSelectedFormId != Guid.Empty &&
            !string.IsNullOrEmpty(ModalSelectedFieldId) &&
            ModalSelectedTargetFieldIds.Any();

        private async Task SaveRuleFromModal()
        {
            if (!CanAddRuleFromModal) return;


            var triggerFieldLabel = GetModalFieldLabelById(ModalSelectedFieldId);
            var targetFieldLabels = ModalSelectedTargetFieldIds.Select(GetModalFieldLabelById).ToList();
            var formName = Forms.FirstOrDefault(f => f.FormId == ModalSelectedFormId)?.Name ?? ModalSelectedFormId.ToString();
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
                        var formId = await FormFieldService.GetFormId(btn.ObjectId);

                        Forms.Add(new FormInfo { FormId = formId, Name = btn.Name, ObjectId = btn.ObjectId });
                    }
                }
            }
            RulesList = await RulesService.GetAllRulesAsync();
        }
    }
}