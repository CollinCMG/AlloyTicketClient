using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace AlloyTicketClient.Components.Pages
{
    public partial class FormModal : ComponentBase
    {
        [Parameter] public bool Show { get; set; }
        [Parameter] public string? Title { get; set; }
        [Parameter] public RequestActionPayload? Payload { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }

        [Inject] private FormFieldService FormFieldService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private AlloyApiService AlloyApiService { get; set; } = default!;
        [Inject] private RulesService RulesService { get; set; } = default!;

        private bool isLoading = false;
        private List<PageDto>? pages;
        private Dictionary<string, object?> fieldValues = new();
        private Guid? lastLoadedFormId = null;
        private bool isSubmitting = false;
        private bool showCancelConfirm = false;
        private bool closeSidebarOnConfirm = false;
        private RuleEvaluationResult? ruleResult;
        private List<string> modifyAppsTriggerFields = new();

        #region Lifecycle
        protected override async Task OnParametersSetAsync()
        {
            if (Show && Payload != null && Guid.TryParse(Payload.FormId, out var formId))
            {
                if (Payload.Data is Dictionary<string, object?> data)
                    fieldValues = data;
                if (lastLoadedFormId != formId || pages == null)
                {
                    isLoading = true;
                    pages = null;
                    pages = await FormFieldService.GetFormPagesAsync(formId);
                    lastLoadedFormId = formId;
                    ruleResult = await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues);
                    var rules = await RulesService.GetRulesForFormAsync(Payload.FormId);
                    modifyAppsTriggerFields = rules
                        .Where(r => r.Action == FilterAction.ModifyApps)
                        .Select(r => r.TriggerField)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    if (ruleResult?.ModifiedApps != null)
                    {
                        foreach (var kvp in ruleResult.ModifiedApps)
                            fieldValues[kvp.Key] = kvp.Value;
                    }
                    await RulesService.EvaluateRulesAsync(Payload.FormId, pages, fieldValues, null);
                    isLoading = false;
                    StateHasChanged();
                }
            }
            else if (!Show)
            {
                fieldValues.Clear();
                pages = null;
                isLoading = false;
                lastLoadedFormId = null;
                ruleResult = null;
                modifyAppsTriggerFields = new();
            }
        }
        #endregion

        #region Event Handlers
        protected async Task SubmitForm()
        {
            isSubmitting = true;
            StateHasChanged();
            try
            {
                if (Payload != null)
                {
                    FormDataMapperService.SetDefaultsForHiddenRequiredFields(pages, fieldValues);
                    var nameKeyed = FormDataMapperService.MapFieldValuesToNameKeyed(pages, fieldValues);
                    Payload.Data = nameKeyed;
                    var json = System.Text.Json.JsonSerializer.Serialize(Payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    await JSRuntime.InvokeVoidAsync("alert", json);
                }
            }
            finally
            {
                isSubmitting = false;
                StateHasChanged();
            }
        }

        protected async Task OnFieldValueChanged(object? value, string fieldKey)
        {
            fieldValues[fieldKey] = value;
            if (Payload != null && pages != null)
            {
                ruleResult = await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues);
                var rules = await RulesService.GetRulesForFormAsync(Payload.FormId);
                if (rules.Any(r => r.Action == FilterAction.ModifyApps && r.TriggerField == fieldKey) && ruleResult?.ModifiedApps != null)
                {
                    foreach (var kvp in ruleResult.ModifiedApps)
                        fieldValues[kvp.Key] = kvp.Value;
                }
                await RulesService.EvaluateRulesAsync(Payload.FormId, pages, fieldValues, fieldKey);
                StateHasChanged();
            }
        }

        protected async Task OnAttachmentChanged(InputFileChangeEventArgs e, string key)
        {
            if (e.FileCount > 0)
                fieldValues[key] = e.File;
            else
                fieldValues.Remove(key);

            if (Payload != null && pages != null)
            {
                ruleResult = await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues);
                if (ruleResult?.ModifiedApps != null)
                {
                    foreach (var kvp in ruleResult.ModifiedApps)
                        fieldValues[kvp.Key] = kvp.Value;
                }
                await RulesService.EvaluateRulesAsync(Payload.FormId, pages, fieldValues, key);
                StateHasChanged();
            }
        }

        protected void HandleOverlayClick()
        {
            if (fieldValues.Count > 0)
            {
                showCancelConfirm = true;
                closeSidebarOnConfirm = true;
            }
            else
            {
                fieldValues.Clear();
                OnClose.InvokeAsync();
            }
        }

        protected void ConfirmCancel()
        {
            showCancelConfirm = false;
            if (closeSidebarOnConfirm)
            {
                fieldValues.Clear();
                OnClose.InvokeAsync();
            }
        }

        protected void CancelCancel()
        {
            showCancelConfirm = false;
            closeSidebarOnConfirm = false;
        }

        protected async Task HandleClose()
        {
            if (fieldValues.Count > 0)
            {
                showCancelConfirm = true;
                closeSidebarOnConfirm = true;
            }
            else
            {
                fieldValues.Clear();
                await OnClose.InvokeAsync();
            }
        }
        #endregion

        #region Helpers
        protected T? GetFieldValue<T>(string key) => fieldValues.TryGetValue(key, out var value) && value is T t ? t : default;

        protected void SetFieldValue<T>(string key, T value) => fieldValues[key] = value;

        protected string GetOrSetFieldValue(string key) => fieldValues.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

        protected void RemoveAttachment(string key) => fieldValues.Remove(key);

        protected bool AreAllRequiredFieldsFilled() =>
            pages != null && pages.Count > 0 &&
            pages.SelectMany(page => page.Items)
                .OfType<FieldInputDto>()
                .Where(f => f.Mandatory == true && !f.IsHidden)
                .All(f => fieldValues.TryGetValue(f.DefinitionID?.ToString() ?? string.Empty, out var value) && !(value == null || string.IsNullOrWhiteSpace(value.ToString())));

        protected List<List<IPageItem>> GetFieldRows(List<IPageItem> items)
        {
            var rows = new List<List<IPageItem>>();
            var currentRow = new List<IPageItem>();
            foreach (var item in items)
            {
                if (item is FieldInputDto fieldInput && (fieldInput.FieldType == FieldType.Memo || (fieldInput.DefinitionID != null && modifyAppsTriggerFields.Contains(fieldInput.DefinitionID.ToString()))))
                {
                    if (currentRow.Count > 0)
                    {
                        rows.Add(new List<IPageItem>(currentRow));
                        currentRow.Clear();
                    }
                    rows.Add(new List<IPageItem> { item });
                }
                else
                {
                    currentRow.Add(item);
                    if (currentRow.Count == 2)
                    {
                        rows.Add(new List<IPageItem>(currentRow));
                        currentRow.Clear();
                    }
                }
            }
            if (currentRow.Count > 0)
                rows.Add(currentRow);
            return rows;
        }
        #endregion
    }
}
