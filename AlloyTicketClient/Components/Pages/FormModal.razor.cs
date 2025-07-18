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

        #endregion
    }
}