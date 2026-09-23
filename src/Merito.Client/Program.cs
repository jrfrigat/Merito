using Flare.Extensions;
using Flare.Theme.MaterialDesign3Expressive;
using Merito.Client;
using Merito.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddFlare(opts =>
{
    opts.DefaultTheme = new MaterialDesign3ExpressiveTheme();
    opts.DefaultPaletteId = Md3Palettes.VioletId;
    opts.RegisterAllBuiltInThemes = false;
});

// The published service worker is registered and updated through Flare's version check.
builder.Services.AddFlareVersionCheck(opts =>
{
    opts.Interval = TimeSpan.FromMinutes(5);
    opts.UseServiceWorker = !builder.HostEnvironment.IsDevelopment();
});

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped(sp => new AuthHandler(sp.GetRequiredService<TokenStore>(), baseAddress)
{
    InnerHandler = new HttpClientHandler(),
});
builder.Services.AddScoped(sp => new ApiClient(new HttpClient(sp.GetRequiredService<AuthHandler>()) { BaseAddress = baseAddress }));
builder.Services.AddScoped<Session>();
builder.Services.AddScoped<NotificationCenter>();

await builder.Build().RunAsync();
