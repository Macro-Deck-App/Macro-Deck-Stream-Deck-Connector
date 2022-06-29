using DeckSurf.SDK.Util;
using Newtonsoft.Json;
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
    internal class ActionButtonModel : IDisposable
    {
        private IntPtr _bufferPtr;
        private int BUFFER_SIZE = 1024 * 1024;
        private bool _disposed = false;

        public ActionButtonModel()
        {
            _bufferPtr = Marshal.AllocHGlobal(BUFFER_SIZE);
        }

        [JsonIgnore]
        public bool IsDisposed
        {
            get
            {
                return this._disposed;
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
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
            _disposed = true;
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
                _iconBase64 = value;
                _iconImage = Utils.Base64.GetImageFromBase64(this.IconBase64);
                if (_iconImage == null) return;
                if (_iconImage.RawFormat.Guid == ImageFormat.Gif.Guid)
                {
                    PropertyItem item = _iconImage.GetPropertyItem(0x5100);
                    _frameDelay =  (item.Value[0] + item.Value[1] * 256) * 10;
                    _frameCount = _iconImage.GetFrameCount(FrameDimension.Time);
                }
                UpdateCurrentFrame();
            }
        }

        public void FrameTick()
        {
            if (_frameDelay > 0 && DateTimeOffset.Now.ToUnixTimeMilliseconds() - _lastFrameUpdate >= _frameDelay)
            {
                _lastFrameUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                _frameIndex++;
                if (_frameIndex >= _frameCount - 1)
                {
                    _frameIndex = 0;
                }
                UpdateCurrentFrame();
            }
        }

        private void UpdateCurrentFrame()
        {
            if (_iconImage == null) return;
            try
            {
                _iconImage.SelectActiveFrame(FrameDimension.Time, _frameIndex);
            }
            catch { }
        }

        public string LabelBase64
        {
            get => _labelBase64;
            set
            {
                _labelBase64 = value;
                _labelBitmap = (Bitmap)Utils.Base64.GetImageFromBase64(_labelBase64);
                UpdateCurrentFrame();
            }
        }

        public string BackgroundColorHex { get; set; } = "#000000";

        [JsonProperty("Position_X")]
        public int Column { get; set; }

        [JsonProperty("Position_Y")]
        public int Row { get; set; }

        [JsonIgnore]
        public Color BackgroundColor
        {
            get => (Color) new ColorConverter().ConvertFromString(this.BackgroundColorHex);
        }

        public byte[] GetCurrentFrame(int size)
        {
            if (IsDisposed) return null;
            try
            {
                Bitmap combined = new Bitmap(size, size, PixelFormat.Format24bppRgb);

                using (Graphics g = Graphics.FromImage(combined))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    if (_iconImage != null)
                    {
                        g.DrawImage(_iconImage, 0, 0, size, size);
                    }
                    if (_labelBitmap != null)
                    {
                        g.DrawImage(_labelBitmap, 0, 0, size, size);
                    }
                }

                combined.RotateFlip(RotateFlipType.Rotate180FlipNone);

                using var bufferStream = new MemoryStream();
                combined.Save(bufferStream, ImageFormat.Jpeg);

                return bufferStream.ToArray();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
