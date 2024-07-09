using System;

namespace MacroDeck.StreamDeckConnector.Parsers;

public static class SerialNumberParser
{
    public static string SerialNumberFromDevicePath(string path)
    {
        path = path.ToUpper();
        var serialNumber = string.Empty;
        try
        {
            if (path.StartsWith(@"\\?\"))
            {
                path = path.Replace(@"\\?\", string.Empty);
                path = path.Replace("#", @"\");
                path = path[..path.IndexOf("{", StringComparison.Ordinal)];
                path = path[..^1];
            }
            serialNumber = path[(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1)..];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse serial number: {ex.Message}");
        }
            
        return serialNumber;
    }
}