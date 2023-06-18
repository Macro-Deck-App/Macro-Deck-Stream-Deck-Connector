using System;

namespace MacroDeck.StreamDeckConnector.Parsers
{
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
                    path = path[..path.IndexOf("{")];
                    path = path[..^1];
                }
                serialNumber = path[(path.LastIndexOf(@"\") + 1)..];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse serial number: {ex.Message}");
            }
            
            return serialNumber;
        }
    }
}