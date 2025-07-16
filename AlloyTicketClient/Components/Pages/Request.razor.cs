using AlloyTicketClient.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace AlloyTicketClient.Components.Pages
{
    public partial class Request : ComponentBase
    {
        [Parameter]
        public string? headerText { get; set; }

        [Inject] private IConfiguration Configuration { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private bool showFormModal = false;
        private string? formModalTitle;
        private RequestActionPayload? formModalPayload;
        private DynamicPageConfig? dynamicPage;

        /// <summary>
        /// Loads the dynamic page configuration based on the headerText route parameter.
        /// </summary>
        protected override void OnParametersSet()
        {
            var dynamicPages = Configuration.GetSection("DynamicPages").Get<List<DynamicPageConfig>>() ?? new();
            if (!string.IsNullOrEmpty(headerText))
            {
                var match = dynamicPages.FirstOrDefault(p => string.Equals(p.HeaderText, Uri.UnescapeDataString(headerText), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    dynamicPage = match;
                }
                else
                {
                    dynamicPage = null;
                }
            }
            else
            {
                dynamicPage = null;
            }
        }

        /// <summary>
        /// Shows the form modal for the selected dynamic button.
        /// </summary>
        private void ShowDynamicButton(DynamicButtonConfig btn)
        {
            formModalTitle = btn.Name + " Request";
            // Create an empty JSON object for Data
            var emptyJson = JsonDocument.Parse("{}").RootElement;
            formModalPayload = new RequestActionPayload
            {
                FormId = btn.FormId,
                Data = emptyJson,
                CategoryId = btn.CategoryId,
                Route = btn.Route
            };
            showFormModal = true;
        }

        /// <summary>
        /// Closes the form modal and resets related state.
        /// </summary>
        private void CloseFormModal()
        {
            showFormModal = false;
            formModalTitle = null;
            formModalPayload = null;
        }
    }
}
