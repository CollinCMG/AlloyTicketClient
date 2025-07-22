using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using AlloyTicketClient.Models.DTOs;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace AlloyTicketClient.Components.Forms
{
    public partial class RequestForm : ComponentBase
    {
        // --- Parameters ---
        [Parameter] public bool Show { get; set; }
        [Parameter] public string? Title { get; set; }
        [Parameter] public RequestActionPayload? Payload { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }

        // --- Injected Services ---
        [Inject] private FormFieldService formFieldService { get; set; }
        [Inject] private AlloyApiService AlloyApiService { get; set; }
        [Inject] private RulesService RulesService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        // --- Private Fields ---
        private bool isLoading = false;
        private List<PageDto>? pages;
        private Dictionary<string, object?> fieldValues = new();
        private Guid? lastLoadedFormId = null;
        private bool isSubmitting = false;
        private bool showCancelConfirm = false;
        private bool closeSidebarOnConfirm = false;
        private RuleEvaluationResult? ruleResult;
        private List<string> modifyAppsTriggerFields = new();

        // --- Lifecycle ---
        protected override async Task OnParametersSetAsync()
        {
            isLoading = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(Payload?.ObjectId))
                {
                    Payload.FormId = await formFieldService.GetFormId(Payload.ObjectId);
                }

                if (Show && Payload != null && Payload.FormId != Guid.Empty)
                {
                    if (Payload.Data.ValueKind == JsonValueKind.Object)
                    {
                        try
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(Payload.Data.GetRawText());
                            if (dict != null)
                                fieldValues = dict;
                        }
                        catch { }
                    }
                    if (lastLoadedFormId != Payload.FormId || pages == null)
                    {
                        pages = await formFieldService.GetFormPagesAsync(Payload.FormId);
                        lastLoadedFormId = Payload.FormId;
                        var rules = await RulesService.GetRulesForFormAsync(Payload.FormId);
                        modifyAppsTriggerFields = rules
                            .Where(r => r.Action == FilterAction.ModifyApps)
                            .Select(r => r.TriggerField)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList();
                        await RulesService.EvaluateRulesAsync(Payload.FormId, pages, fieldValues, null);
                        StateHasChanged();
                    }
                }
                else if (!Show)
                {
                    fieldValues.Clear();
                    pages = null;
                    lastLoadedFormId = null;
                    ruleResult = null;
                    modifyAppsTriggerFields = new();
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        // --- Event Handlers ---
        private async Task SubmitFormAsync()
        {
            isSubmitting = true;
            StateHasChanged();
            try
            {
                if (Payload != null)
                {
                    FormDataMapperService.SetDefaultsForHiddenRequiredFields(pages, fieldValues);
                    var namedKeys = FormDataMapperService.MapFieldValuesToNameKeyed(pages, fieldValues);
                    var json = JsonSerializer.Serialize(namedKeys);
                    JsonDocument doc = JsonDocument.Parse(json);
                    Payload.Data = doc.RootElement;
                    var (success, message) = await AlloyApiService.PostAsync(Payload);
                    await JSRuntime.InvokeVoidAsync("alert", $"API call result: {message}");
                }
            }
            finally
            {
                isSubmitting = false;
                StateHasChanged();
            }
        }

        private async Task OnFieldValueChanged(object? value, string fieldKey)
        {
            fieldValues[fieldKey] = value;
            if (Payload != null && pages != null)
            {
                await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues, fieldKey);
                await RulesService.GetRulesForFormAsync(Payload.FormId); // result not used, can be removed if not needed
                await RulesService.EvaluateRulesAsync(Payload.FormId, pages, fieldValues, fieldKey);
                StateHasChanged();
            }
        }

        private void HandleOverlayClick()
        {
            if (fieldValues.Count > 0)
            {
                showCancelConfirm = true;
                closeSidebarOnConfirm = true;
            }
            else
            {
                fieldValues.Clear();
                if (OnClose.HasDelegate)
                    OnClose.InvokeAsync();
            }
        }

        private void ConfirmCancel()
        {
            showCancelConfirm = false;
            if (closeSidebarOnConfirm)
            {
                fieldValues.Clear();
                if (OnClose.HasDelegate)
                    OnClose.InvokeAsync();
            }
        }

        private void CancelCancel()
        {
            showCancelConfirm = false;
            closeSidebarOnConfirm = false;
        }

        private async Task HandleClose()
        {
            if (fieldValues.Count > 0)
            {
                showCancelConfirm = true;
                closeSidebarOnConfirm = true;
            }
            else
            {
                fieldValues.Clear();
                if (OnClose.HasDelegate)
                    await OnClose.InvokeAsync();
            }
        }

        // --- Helpers ---
        private List<List<IPageItem>> GetFieldRows(List<IPageItem> items)
        {
            var rows = new List<List<IPageItem>>();
            var currentRow = new List<IPageItem>();
            foreach (var item in items)
            {
                if ((item is FieldInputDto fieldInput && (fieldInput.FieldType == FieldType.Memo || (fieldInput.DefinitionID != null && modifyAppsTriggerFields.Contains(fieldInput.DefinitionID.ToString()))))
                    || item is FieldTextDto)
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

        private bool RowNeedsFullWidth(List<IPageItem> row)
        {
            if (row.Count == 1 && row[0] is FieldInputDto fieldInput)
            {
                var mapped = FormDataMapperService.MapToFormFieldDto(fieldInput);
                if (mapped.FieldType == FieldType.Input && !string.IsNullOrWhiteSpace(mapped.LookupValues))
                {
                    var (lookupType, options) = ParseLookupTypeAndValues(mapped.LookupValues);
                    if (lookupType == 1 && options.Count > 2)
                        return true;
                }
            }
            return false;
        }

        private (int lookupType, List<string> options) ParseLookupTypeAndValues(string lookupValues)
        {
            var parts = lookupValues.Split(',', 2);
            if (parts.Length < 2 || !int.TryParse(parts[0], out var type))
            {
                return (2, lookupValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
            }
            var rest = parts[1];
            var options = new List<string>();
            var quoteStart = rest.IndexOf('"');
            var quoteEnd = rest.LastIndexOf('"');
            if (quoteStart >= 0 && quoteEnd > quoteStart)
            {
                var quoted = rest.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                options = quoted.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.Replace("\"", string.Empty)).ToList();
            }
            else
            {
                options = rest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.Replace("\"", string.Empty)).ToList();
            }
            return (type, options.Distinct().ToList());
        }
    }
}
