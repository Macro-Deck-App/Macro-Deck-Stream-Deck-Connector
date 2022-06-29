using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace MacroDeck.StreamDeckConnector.Utils
{
    public class CombineBitmaps
    {
        public static byte[] CombineAll(Bitmap[] bitmaps, int size)
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
                    foreach (Bitmap bitmap in bitmaps.Where(x => x != null))
                    {
                        g.DrawImage(bitmap, 0, 0, size, size);
                    }
                }

                combined.RotateFlip(RotateFlipType.Rotate180FlipNone);

                using var bufferStream = new MemoryStream();
                combined.Save(bufferStream, ImageFormat.Jpeg);

                return bufferStream.ToArray();
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}
