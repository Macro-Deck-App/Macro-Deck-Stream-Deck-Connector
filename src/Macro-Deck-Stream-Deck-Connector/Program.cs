using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUsbEventWatcher>(new UsbEventWatcher(includeTTY: true));
                services.AddHostedService<UsbHostedService>();
            }).UseSerilog((_, _, configuration) =>
            {
                configuration
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code);
            })
            .Build();
        
        await app.RunAsync();
    }
}