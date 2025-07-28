using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using AlloyTicketClient.Models.DTOs;
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
        private Guid? ModalSelectedFormId;
        private string? ModalSelectedFieldId, ModalTriggerValue;

        private FilterAction ModalSelectedAction { get; set; } = FilterAction.Hide;
        private List<string> ModalSelectedTargetFieldIds { get; set; } = new();
        private List<FieldInputDto> ModalFormFields { get; set; } = new();

        // Modal state for FieldsByRole
        private RoleName? ModalSelectedRoleName { get; set; } = null;
        private bool ModalIsQueue { get; set; } = false;
        private string? ModalTargetValueOverride { get; set; } = null;

        // Modal state for Hide
        private bool ModalAlwaysHide { get; set; } = false;

        private IEnumerable<RoleName> AllRoles => Enum.GetValues(typeof(RoleName)).Cast<RoleName>();

        private async Task OpenRuleModal(RuleConfig? rule)
        {
            ShowRuleModal = true;
            IsEditMode = rule != null;
            if (rule == null)
            {
                EditingRule = null;
                ModalSelectedFieldId = null;
                ModalSelectedFormId = null;
                ModalSelectedAction = FilterAction.Hide;
                ModalSelectedTargetFieldIds.Clear();
                ModalFormFields.Clear();
                ModalTriggerValue = null;
                ModalSelectedRoleName = null;
                ModalIsQueue = false;
                ModalTargetValueOverride = null;
                ModalAlwaysHide = false;
            }
            else
            {
                EditingRule = RulesList.FirstOrDefault(r => r.RuleId == rule.RuleId);
                ModalSelectedFormId = EditingRule?.FormId ?? Guid.Empty;
                ModalSelectedFieldId = EditingRule?.TriggerField;
                ModalSelectedAction = EditingRule?.Action ?? FilterAction.Hide;
                ModalSelectedTargetFieldIds = EditingRule?.TargetList.Select(t => t.FieldId).ToList() ?? new();
                ModalTriggerValue = EditingRule?.TriggerValue;
                ModalAlwaysHide = EditingRule?.AlwaysHide ?? false;
                ModalTargetValueOverride = EditingRule?.TargetValueOverride;
                if (ModalSelectedAction == FilterAction.ModifyApps)
                {
                    ModalTargetValueOverride = EditingRule?.TargetValueOverride;
                }
                else if (ModalSelectedAction == FilterAction.FieldsByRole)
                {
                    ModalSelectedRoleName = EditingRule?.RoleName;
                    ModalIsQueue = EditingRule?.IsQueue ?? false;
                    ModalTargetValueOverride = EditingRule?.TargetValueOverride;
                }
                else
                {
                    ModalSelectedRoleName = null;
                    ModalIsQueue = false;
                    ModalTargetValueOverride = null;
                }
                if (ModalSelectedFormId != null && ModalSelectedFormId != Guid.Empty)
                {
                    var pages = await FormFieldService.GetFormPagesAsync(ModalSelectedFormId.Value);
                    var fields = pages.SelectMany(p => p.Items).OfType<FieldInputDto>();
                    ModalFormFields = fields
                        .Where(f => f.FieldType != FieldType.Text)
                        .ToList();
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
            if (ModalSelectedFormId != null && ModalSelectedFormId != Guid.Empty)
            {
                var pages = await FormFieldService.GetFormPagesAsync(ModalSelectedFormId.Value);
                ModalFormFields = pages.SelectMany(p => p.Items).OfType<FieldInputDto>().ToList();
            }
            StateHasChanged();
        }

        private void OnModalTargetFieldsChanged(ChangeEventArgs e)
        {
            if (ModalSelectedAction == FilterAction.FieldsByRole)
            {
                // Single select
                if (e.Value is string single && !string.IsNullOrEmpty(single))
                    ModalSelectedTargetFieldIds = new() { single };
                else
                    ModalSelectedTargetFieldIds = new();
            }
            else
            {
                // Multi-select
                if (e.Value is string single)
                    ModalSelectedTargetFieldIds = new() { single };
                else if (e.Value is IEnumerable<string> selectedOptions)
                    ModalSelectedTargetFieldIds = selectedOptions.ToList();
                else
                    ModalSelectedTargetFieldIds = new();
            }
        }

        private bool CanAddRuleFromModal =>
            ModalSelectedFormId != Guid.Empty &&
            ModalSelectedTargetFieldIds.Any() &&
            (ModalSelectedAction != FilterAction.Hide || ModalAlwaysHide || !string.IsNullOrEmpty(ModalSelectedFieldId));

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
                        FieldType = field?.FieldType ?? FieldType.Null,
                        FieldValue = field?.FieldValue
                    };
                })
                .ToList();
            RoleName? roleNameToSave = null;
            bool isQueueToSave = false;
            string? targetValueOverrideToSave = null;
            if (ModalSelectedAction == FilterAction.FieldsByRole || ModalSelectedAction == FilterAction.ModifyApps || ModalSelectedAction == FilterAction.Hide)
            {
                roleNameToSave = ModalSelectedAction == FilterAction.FieldsByRole ? ModalSelectedRoleName : null;
                isQueueToSave = ModalSelectedAction == FilterAction.FieldsByRole ? ModalIsQueue : false;
                targetValueOverrideToSave = string.IsNullOrWhiteSpace(ModalTargetValueOverride) ? null : ModalTargetValueOverride;
            }
            if (ModalSelectedAction == FilterAction.Hide && ModalAlwaysHide)
            {
                ModalSelectedFieldId = null;
                ModalTriggerValue = null;
            }
            if (IsEditMode && EditingRule != null)
            {
                EditingRule.FormId = ModalSelectedFormId ?? Guid.Empty;
                EditingRule.FormName = formName;
                EditingRule.TriggerField = ModalSelectedFieldId;
                EditingRule.TriggerFieldLabel = triggerFieldLabel;
                EditingRule.TriggerValue = string.IsNullOrWhiteSpace(ModalTriggerValue) ? null : ModalTriggerValue;
                EditingRule.Action = ModalSelectedAction;
                EditingRule.TargetList = targetList;
                EditingRule.TargetFieldLabels = targetFieldLabels;
                EditingRule.RoleName = roleNameToSave;
                EditingRule.IsQueue = isQueueToSave;
                EditingRule.TargetValueOverride = targetValueOverrideToSave;
                EditingRule.AlwaysHide = ModalAlwaysHide;
                await RulesService.UpdateRuleAsync(EditingRule);
            }
            else
            {
                var rule = new RuleConfig
                {
                    RuleId = Guid.NewGuid(),
                    FormId = ModalSelectedFormId ?? Guid.Empty,
                    FormName = formName,
                    TriggerField = ModalSelectedFieldId,
                    TriggerFieldLabel = triggerFieldLabel,
                    TriggerValue = string.IsNullOrWhiteSpace(ModalTriggerValue) ? null : ModalTriggerValue,
                    Action = ModalSelectedAction,
                    TargetList = targetList,
                    TargetFieldLabels = targetFieldLabels,
                    RoleName = roleNameToSave,
                    IsQueue = isQueueToSave,
                    TargetValueOverride = targetValueOverrideToSave,
                    AlwaysHide = ModalAlwaysHide
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

        protected override async Task OnInitializedAsync()
        {
            var dynamicPages = Configuration.GetSection("DynamicPages").Get<List<DynamicPageConfig>>() ?? new();
            foreach (var page in dynamicPages)
            {
                if (page.Buttons != null)
                {
                    foreach (var btn in page.Buttons)
                    {
                        var formId = Guid.Empty;
                        if (!string.IsNullOrWhiteSpace(btn.ObjectId))
                        {
                            formId = await FormFieldService.GetFormIdByObjectId(btn.ObjectId);
                        }
                        else if (btn.ActionId != null)
                        {
                            formId = await FormFieldService.GetFormIdByActionId(btn.ActionId);
                        }

                        Forms.Add(new FormInfo { FormId = formId, Name = btn.Name, ObjectId = btn.ObjectId });
                    }
                }
            }
            RulesList = await RulesService.GetAllRulesAsync();
        }
    }
}