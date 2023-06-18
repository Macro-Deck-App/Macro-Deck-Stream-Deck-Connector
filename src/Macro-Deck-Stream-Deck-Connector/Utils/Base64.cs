using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MacroDeck.StreamDeckConnector.Utils
{
    public class Base64
    {
        
        public static Image GetImageFromBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            try
            {
                HashSet<char> whiteSpace = new HashSet<char> { '\t', '\n', '\r', ' ' };
                int length = base64.Count(c => !whiteSpace.Contains(c));
                if (length % 4 != 0)
                    base64 += new string('=', 4 - length % 4);
                byte[] imageBytes = Convert.FromBase64String(base64);

                var ms = new MemoryStream(imageBytes);
                Image image = image = Image.FromStream(ms, true);

                return image;
            } catch
            {
                return null;
            }
            
        }

        public static string GetBase64FromImage(Image image)
        {
            if (image == null) return "";
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var format = image.RawFormat;
                    switch (format.ToString().ToLower())
                    {
                        case "gif":
                            break;
                        default:
                            image = new Bitmap(image); // Generating a new bitmap if the file format is not a gif because otherwise it causes a GDI+ error in some cases
                            format = ImageFormat.Png;
                            break;
                    }
                    image.Save(ms, format);
                    image.Dispose();

                    return Convert.ToBase64String(ms.ToArray());
                }
            } catch
            {
                return "";
            }
           
        }

    }
}
