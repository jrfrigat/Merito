using Flare.Extensions;
using Flare.Theme.MaterialDesign3Expressive;
using Merito.Client;
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

await builder.Build().RunAsync();
