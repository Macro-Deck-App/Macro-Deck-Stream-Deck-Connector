// Copyright (c) Den Delimarsky
// Den Delimarsky licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using DeckSurf.SDK.Models;

namespace DeckSurf.SDK.Util
{
    /// <summary>
    /// Collection of methods used for image manipulation, allowing easier Stream Deck button image preparation.
    /// </summary>
    public class ImageHelpers
    {

        public static byte[] ResizeImage(Image image, int width, int height)
        {
            try
            {
                var targetRectangle = new Rectangle(0, 0, width, height);
                var targetImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                targetImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (var graphics = Graphics.FromImage(targetImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    graphics.DrawImage(image, targetRectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                }

                // TODO: I am not sure if every image needs to be rotated, but
                // in my limited experiments, this seems to be the case.
                targetImage.RotateFlip(RotateFlipType.Rotate180FlipNone);

                using var bufferStream = new MemoryStream();
                targetImage.Save(bufferStream, ImageFormat.Jpeg);
                return bufferStream.ToArray();
            } catch
            {
                return null;
            }
           
        }


    }
}
