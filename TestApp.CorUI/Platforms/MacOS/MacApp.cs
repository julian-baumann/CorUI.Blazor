using CorUI;
using CorUI.macOS;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestApp.macOS;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services
    .AddCorUINative<App>(
        new BlazorWebViewOptions
        {
            RootComponent = typeof(TestApp.App)
        }
    );

builder.Services
    .AddMacOS()
    .AddRazorComponents();

var app = builder.Build();

app.UseDefaultFiles();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapRazorComponents<TestApp.App>();

await app.RunAsync();