using CorUI.macOS;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestApp.macOS;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        
        builder.Services.AddMacOS(
            new BlazorWebViewOptions
            {
                RootComponent = typeof(TestApp.App),
                HostPath = "wwwroot/index.html"
            }
        );
        
        builder.Services
            .AddHostedService<MacOSApplication>()
            .AddRazorComponents();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.MapRazorComponents<TestApp.App>();
        await app.RunAsync();
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}