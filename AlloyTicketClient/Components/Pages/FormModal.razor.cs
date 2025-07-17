using AlloyTicketClient.Enums;
using AlloyTicketClient.Models;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text.Json;

namespace AlloyTicketClient.Components.Pages
{
    public partial class FormModal : ComponentBase
    {
        [Parameter] public bool Show { get; set; }
        [Parameter] public string? Title { get; set; }
        [Parameter] public RequestActionPayload? Payload { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }

        [Inject] private FormFieldService formFieldService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private AlloyApiService AlloyApiService { get; set; } = default!;
        [Inject] private RulesService RulesService { get; set; } = default!;

        private List<PageDto>? pages;
        private Dictionary<string, object?> fieldValues = new();
        private Guid? lastLoadedFormId = null;
        private bool showCancelConfirm = false;
        private bool closeSidebarOnConfirm = false;

        #region Lifecycle
        protected override async Task OnParametersSetAsync()
        {
            Guid? formId = null;
            if (!string.IsNullOrWhiteSpace(Payload?.ObjectId) && Guid.TryParse(Payload.ObjectId, out var objectGuid))
            {
                formId = objectGuid;
            }
            else if (Payload?.FormId != Guid.Empty)
            {
                formId = Payload?.FormId;
            }
            if (Show && Payload != null && formId != null)
            {
                if (Payload.Data.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(Payload.Data.GetRawText());
                        if (dict != null)
                            fieldValues = dict;
                    }
                    catch { /* ignore deserialization errors, fallback to empty */ }
                }
                if (lastLoadedFormId != formId || pages == null)
                {
                    pages = null;
                    pages = await formFieldService.GetFormPagesAsync(formId.Value);
                    lastLoadedFormId = formId;
                    await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues);
                    var rules = await RulesService.GetRulesForFormAsync(Payload.FormId);
               
                    await RulesService.EvaluateRulesAsync(Payload.FormId, pages, fieldValues, null);
                    StateHasChanged();
                }
            }
            else if (!Show)
            {
                fieldValues.Clear();
                pages = null;
                lastLoadedFormId = null;
            }
        }
        #endregion

        #region Event Handlers
        protected async Task SubmitForm()
        {
            StateHasChanged();
            try
            {
                if (Payload != null)
                {
                    FormDataMapperService.SetDefaultsForHiddenRequiredFields(pages, fieldValues);
                    var nameKeyed = FormDataMapperService.MapFieldValuesToNameKeyed(pages, fieldValues);

                    // Add attachments to payload using FieldName as key and UploadedFileContentBase64 as value
                    if (pages != null)
                    {
                        foreach (var page in pages)
                        {
                            foreach (var item in page.Items)
                            {
                                if (item is AttachmentInputDto attachment &&
                                    !string.IsNullOrWhiteSpace(attachment.FieldName) &&
                                    !string.IsNullOrWhiteSpace(attachment.UploadedFileContentBase64))
                                {
                                    nameKeyed[attachment.FieldName] = attachment.UploadedFileContentBase64;
                                }
                            }
                        }
                    }

                    // Convert the dictionary to JsonElement
                    var json = JsonSerializer.Serialize(nameKeyed);
                    Payload.Data = JsonDocument.Parse(json).RootElement;
                    // Call AlloyApiService and show the result
                    var (success, message) = await AlloyApiService.PostAsync(Payload);
                    await JSRuntime.InvokeVoidAsync("alert", $"API call result: {message}");
                }
            }
            finally
            {
                StateHasChanged();
            }
        }

        protected async Task OnFieldValueChanged(object? value, string fieldKey)
        {
            fieldValues[fieldKey] = value;
            if (Payload != null && pages != null)
            {
                await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues);
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
                await RulesService.EvaluateModifyAppsRulesAsync(Payload.FormId, fieldValues);
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


        protected List<List<IPageItem>> GetFieldRows(List<IPageItem> items)
        {
            var rows = new List<List<IPageItem>>();
            var currentRow = new List<IPageItem>();
            foreach (var item in items)
            {
                if (item is FieldInputDto fieldInput && (fieldInput.FieldType == FieldType.Memo || (fieldInput.DefinitionID != null)))
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