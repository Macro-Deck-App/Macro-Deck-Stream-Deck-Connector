using System;
using CommandLine;

namespace MacroDeck.StreamDeckConnector.Setup;

public class StartParameters
{
    [Option("host", Default = "127.0.0.1:8191", Required = false)]
    public string Host { get; set; } = "127.0.0.1:8191";
    
    [Option("longpress", Default = 1000, Required = false)]
    public int LongPressDelay { get; set; }
    
    [Option("wss", Default = false, Required = false)]
    public bool WebSocketSecure { get; set; }

    public static StartParameters ParseParameters()
    {
        var args = Environment.GetCommandLineArgs();
        var startParameters = new StartParameters();
        if (args.Length == 0)
        {
            return startParameters;
        }

        using var parser = new Parser(options =>
        {
            options.HelpWriter = Console.Error;
            options.IgnoreUnknownArguments = true;
            options.EnableDashDash = true;
        });

        parser.ParseArguments<StartParameters>(args)
            .WithParsed(sp => startParameters = sp);

        return startParameters;
    }

    public static StartParameters Instance { get; } = ParseParameters();
}