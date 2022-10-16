using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace MacroDeck.StreamDeckConnector.Utils
{
    public class EmptyButtonImageGenerator
    {
        public static KeyBitmap GetEmptyButton(int size)
        {
            try
            {
                var combined = new Bitmap(size, size, PixelFormat.Format24bppRgb);

                const int iconPosition = 0;

                using (var g = Graphics.FromImage(combined))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    
                    using var brush = new SolidBrush(Color.FromArgb(35, 35, 35));
                    g.FillRectangle(brush, iconPosition, iconPosition, size, size);
                }
                
                using var bufferStream = new MemoryStream();
                combined.Save(bufferStream, ImageFormat.Png);

                return KeyBitmap.Create.FromStream(bufferStream);
            }
            catch
            {
                return KeyBitmap.Black;
            }

        }

    }
}
