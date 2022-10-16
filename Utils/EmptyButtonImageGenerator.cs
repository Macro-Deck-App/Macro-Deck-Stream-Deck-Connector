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
        public static KeyBitmap GetEmptyButton(int size, bool cropped = false)
        {
            try
            {
                Bitmap combined = new Bitmap(size, size, PixelFormat.Format24bppRgb);

                using (Graphics g = Graphics.FromImage(combined))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    int iconPosition = cropped ? 10 : 0;
                    int iconSize = cropped ? size - 20 : size;


                    using SolidBrush brush = new SolidBrush(Color.FromArgb(35, 35, 35));
                    g.FillRectangle(brush, iconPosition, iconPosition, iconSize, iconSize);

                }

                combined.RotateFlip(RotateFlipType.Rotate180FlipNone);

                using var bufferStream = new MemoryStream();
                combined.Save(bufferStream, ImageFormat.Png);

                return KeyBitmap.Create.FromStream(bufferStream);
            }
            catch
            {
                return null;
            }
        }

    }
}
