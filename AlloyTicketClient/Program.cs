using AlloyTicketClient.Components;
using AlloyTicketClient.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using AlloyTicketClient.Contexts;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddMicrosoftIdentityConsentHandler();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AlloyNavigatorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AlloyNavigator")));

builder.Services.AddDbContext<AlloyTicketRulesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AlloyTicketRulesDb")));

builder.Services.AddScoped<FormFieldService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AlloyApiService>();
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<RulesService>();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.UseAuthentication();
app.UseAuthorization();
app.Run();
