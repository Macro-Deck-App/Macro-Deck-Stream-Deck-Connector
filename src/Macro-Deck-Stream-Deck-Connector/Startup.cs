using MacroDeck.StreamDeckConnector.HostedServices;
using MacroDeck.StreamDeckConnector.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<StartParameters>();
        services.AddSingleton(new UsbEventWatcher(includeTTY: true));
        services.AddHostedService<UsbHostedService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}