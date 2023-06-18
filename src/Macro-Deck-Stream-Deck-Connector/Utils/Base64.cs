using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MacroDeck.StreamDeckConnector.Utils;

public class Base64
{
    public static Image? GetImageFromBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }
        try
        {
            var whiteSpace = new HashSet<char> { '\t', '\n', '\r', ' ' };
            var length = base64.Count(c => !whiteSpace.Contains(c));

            if (length % 4 != 0)
            {
                base64 += new string('=', 4 - length % 4);
            }

            var imageBytes = Convert.FromBase64String(base64);

            var ms = new MemoryStream(imageBytes);
            
            return Image.FromStream(ms, true);
        }
        catch
        {
            return null;
        }
            
    }
}