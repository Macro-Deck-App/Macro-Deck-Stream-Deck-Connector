using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MacroDeck.StreamDeckConnector;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(hostBuilder =>
            {
                hostBuilder.UseStartup<Startup>();
            }).Build();
        
        await app.RunAsync();
    }
}