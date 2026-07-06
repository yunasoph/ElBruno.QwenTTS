using ElBruno.QwenTTS.Web.Components;
using ElBruno.QwenTTS.Web.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ADDED THIS: So the app knows to use our TtsController.cs file!
builder.Services.AddControllers(); 

builder.Services.AddSignalR(options =>
{
    // Allow large JS interop messages (recorded audio returned as base64)
    options.MaximumReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
});
builder.Services.AddSingleton<TtsPipelineService>();
builder.Services.AddSingleton<VoiceClonePipelineService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ADDED THIS: This allows Lovable to talk to your server without browser security errors
app.UseCors(); 

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseAntiforgery();

// Serve runtime-generated files (WAV audio, references) via dedicated static file providers.
var webRootPath = app.Environment.WebRootPath;
var generatedDir = Path.Combine(webRootPath, "generated");
var referencesDir = Path.Combine(webRootPath, "references");
Directory.CreateDirectory(generatedDir);
Directory.CreateDirectory(referencesDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(generatedDir),
    RequestPath = "/generated"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(referencesDir),
    RequestPath = "/references"
});

app.MapStaticAssets();

// ADDED THIS: This tells the app to actually turn on our API routes!
app.MapControllers(); 

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
