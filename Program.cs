using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.ResponseCompression;
using ScrumPoker.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Content root ve web root path'leri ayarla
var contentRoot = Directory.GetCurrentDirectory();
var webRoot = Path.Combine(contentRoot, "wwwroot");

builder.Environment.ContentRootPath = contentRoot;
builder.WebHost.UseWebRoot(webRoot);
builder.WebHost.UseContentRoot(contentRoot);

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400;
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(30);
    options.KeepAliveInterval = TimeSpan.FromMinutes(15);
    options.HandshakeTimeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<ScrumPokerHub>("/planningpokerhub");
app.MapFallbackToPage("/_Host");

app.Run();
