var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();
