using AlloyTicketClient.Components;
using AlloyTicketClient.Contexts;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddMicrosoftIdentityConsentHandler();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AlloyTicketRulesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AlloyTicketRulesDb")));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AlloyApiService>();
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<RulesService>();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
});

builder.Services.AddControllers(); // Register MVC controllers for Microsoft Identity endpoints

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting(); // Ensure routing is enabled before authentication/authorization
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers(); // Map controllers before Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
