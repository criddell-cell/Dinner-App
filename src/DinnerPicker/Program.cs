using DinnerPicker.Persistence;
using DinnerPicker.Services;
using DinnerPicker.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Core app services
builder.Services.AddSingleton<IDataStore, JsonDataStore>();
builder.Services.AddSingleton<PantryService>();
builder.Services.AddSingleton<IPantryService>(sp => sp.GetRequiredService<PantryService>());
builder.Services.AddSingleton<IHistoryService, HistoryService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddTransient<ISuggestionService, SuggestionService>();
builder.Services.AddTransient<IScanService, ScanService>();
builder.Services.AddScoped<SettingsNavigator>();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Tell Cloudflare/browsers not to cache CSS/JS so updates show immediately
        if (ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js"))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
        }
    }
});
app.UseAntiforgery();

// Serve meal photos stored outside wwwroot
app.MapGet("/meal-photos/{filename}", (string filename) =>
{
    var dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".dinnerpicker", "images");
    var path = Path.Combine(dir, filename);
    if (!File.Exists(path)) return Results.NotFound();
    var ext = Path.GetExtension(filename).ToLowerInvariant();
    var mime = ext switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
    return Results.File(path, mime);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
