using Newtonsoft.Json;
using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MacroDeck.StreamDeckConnector.Models
{
    internal sealed class ActionButtonModel : IDisposable
    {
        private readonly IntPtr _bufferPtr;
        private int BUFFER_SIZE = 1024 * 1024;

        public ActionButtonModel()
        {
            _bufferPtr = Marshal.AllocHGlobal(BUFFER_SIZE);
        }

        [JsonIgnore]
        private bool IsDisposed { get; set; } = false;

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
            }

            if (_iconImage != null)
            {
                _iconImage.Dispose();
                _iconImage = null;
            }

            if (_labelBitmap != null)
            {
                _labelBitmap.Dispose();
                _labelBitmap = null;
            }

            Marshal.FreeHGlobal(_bufferPtr);
            IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ActionButtonModel()
        {
            Dispose(false);
        }

        private readonly object _iconLock = new object();
        private readonly object _labelLock = new object();

        private string _iconBase64;

        private string _labelBase64;

        private Image _iconImage;

        private Bitmap _labelBitmap;

        private int _frameIndex = 0;

        private int _frameCount = 0;

        private int _frameDelay = 0;

        private long _lastFrameUpdate = 0;
        
        public string IconBase64
        {
            get => _iconBase64;
            set
            {
                if (_iconBase64 == value) return;
                lock (_iconLock)
                {
                    _iconBase64 = value;
                    _iconImage = Utils.Base64.GetImageFromBase64(IconBase64);
                    if (_iconImage == null) return;
                    if (_iconImage.RawFormat.Guid == ImageFormat.Gif.Guid)
                    {
                        var item = _iconImage.GetPropertyItem(0x5100);
                        _frameDelay =  (item.Value[0] + item.Value[1] * 256) * 10;
                        _frameCount = _iconImage.GetFrameCount(FrameDimension.Time);
                    }
                    _frameIndex = 0;
                    UpdateCurrentFrame();
                }
            }
        }
        
        public string LabelBase64
        {
            get => _labelBase64;
            set
            {
                if (_labelBase64 == value) return;
                lock (_labelLock)
                {
                    _labelBase64 = value;
                    _labelBitmap = (Bitmap)Utils.Base64.GetImageFromBase64(_labelBase64);
                    UpdateCurrentFrame();
                }
            }
        }

        public void FrameTick()
        {
            if (_frameDelay <= 0 ||
                DateTimeOffset.Now.ToUnixTimeMilliseconds() - _lastFrameUpdate < _frameDelay) return;
            _lastFrameUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            _frameIndex++;
            if (_frameIndex >= _frameCount - 1)
            {
                _frameIndex = 0;
            }
            UpdateCurrentFrame();
        }

        private void UpdateCurrentFrame()
        {
            try
            {
                lock (_iconLock)
                {
                    if (_iconImage == null) return;
                    _iconImage.SelectActiveFrame(FrameDimension.Time, _frameIndex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while updating frame: {ex.Message}");
            }
        }


        public string BackgroundColorHex { get; set; } = "#000000";

        [JsonProperty("Position_X")]
        public int Column { get; set; }

        [JsonProperty("Position_Y")]
        public int Row { get; set; }

        [JsonIgnore]
        private Color BackgroundColor => (Color) new ColorConverter().ConvertFromString(BackgroundColorHex);

        public KeyBitmap? GetCurrentFrame(int size)
        {
            if (IsDisposed) return null;
            try
            {
                var combined = new Bitmap(size, size, PixelFormat.Format24bppRgb);

                using (var g = Graphics.FromImage(combined))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    const int iconPosition = 0;

                    using (var brush = new SolidBrush(BackgroundColor))
                    {
                        g.FillRectangle(brush, iconPosition, iconPosition, size, size);
                    }

                    lock (_iconLock)
                    {
                        if (_iconImage != null)
                        {
                            g.DrawImage(_iconImage, iconPosition, iconPosition, size, size);
                        }
                    }

                    lock (_labelLock)
                    {
                        if (_labelBitmap != null)
                        {
                            g.DrawImage(_labelBitmap, iconPosition, iconPosition, size, size);
                        }
                    }
                    
                }

                using var bufferStream = new MemoryStream();
                combined.Save(bufferStream, ImageFormat.Png);

                return KeyBitmap.Create.FromStream(bufferStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}
