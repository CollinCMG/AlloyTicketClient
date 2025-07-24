using System.Text.Json;
using AlloyTicketClient.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AlloyTicketClient.Components.Pages
{
    public partial class Request : ComponentBase
    {
        // --- Parameters ---
        [Parameter]
        [SupplyParameterFromQuery(Name = "type")]
        public string? type { get; set; }

        // --- Injected Services ---
        [Inject] private IConfiguration Configuration { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        // --- Private Fields ---
        private bool showFormModal = false;
        private string? formModalTitle;
        private RequestActionPayload? formModalPayload;
        private DynamicPageConfig? dynamicPage;
        private string? userDisplayName;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            userDisplayName = authState.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? authState.User.Identity?.Name
                ?? string.Empty;
        }

        /// <summary>
        /// Loads the dynamic page configuration based on the 'type' query parameter.
        /// </summary>
        protected override void OnParametersSet()
        {
            var dynamicPages = Configuration.GetSection("DynamicPages").Get<List<DynamicPageConfig>>() ?? new();
            if (!string.IsNullOrEmpty(type))
            {
                var match = dynamicPages.FirstOrDefault(p => string.Equals(p.HeaderText, Uri.UnescapeDataString(type), StringComparison.OrdinalIgnoreCase));
                dynamicPage = match;
            }
            else
            {
                dynamicPage = null;
            }
        }

        /// <summary>
        /// Shows the form modal for the selected dynamic button.
        /// </summary>
        private Task ShowDynamicButtonAsync(DynamicButtonConfig btn)
        {
            formModalTitle = btn.Name;
            var emptyJson = JsonDocument.Parse("{}" ).RootElement;
            formModalPayload = new RequestActionPayload
            {
                Requester_ID = userDisplayName ?? string.Empty,
                FormId = btn.FormId,
                Data = emptyJson,
                ObjectId = btn.ObjectId
            };
            showFormModal = true;
            return Task.CompletedTask;
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
