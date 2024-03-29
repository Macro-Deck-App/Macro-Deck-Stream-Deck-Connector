﻿// Copyright (c) Den Delimarsky
// Den Delimarsky licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DeckSurf.SDK.Core;
using DeckSurf.SDK.Util;
using HidSharp;
using MacroDeck.StreamDeckConnector;

namespace DeckSurf.SDK.Models
{
    /// <summary>
    /// Abstract class representing a connected Stream Deck device. Use specific implementations for a given connected model.
    /// </summary>
    public abstract class ConnectedDevice
    {
        private const int ButtonPressHeaderOffset = 4;

        private static readonly int ImageReportLength = 1024;
        private static readonly int ImageReportHeaderLength = 8;
        private static readonly int ImageReportPayloadLength = ImageReportLength - ImageReportHeaderLength;

        private byte[] keyPressBuffer = new byte[1024];

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedDevice"/> class.
        /// </summary>
        public ConnectedDevice()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedDevice"/> class with given device parameters.
        /// </summary>
        /// <param name="vid">Vendor ID.</param>
        /// <param name="pid">Product ID.</param>
        /// <param name="path">Path to the USB HID device.</param>
        /// <param name="name">Name of the USB HID device.</param>
        /// <param name="model">Stream Deck model.</param>
        public ConnectedDevice(int vid, int pid, string path, string name, DeviceModel model, string serialNumber)
        {
            this.VId = vid;
            this.PId = pid;
            this.Path = path;
            this.Name = name;
            this.Model = model;
            this.SerialNumber = serialNumber;
            this.UnderlyingDevice = DeviceList.Local.GetHidDeviceOrNull(this.VId, this.PId);

            this.ButtonCount = model switch
            {
                DeviceModel.XL => DeviceConstants.XLButtonCount,
                DeviceModel.MINI => DeviceConstants.MiniButtonCount,
                DeviceModel.ORIGINAL => DeviceConstants.OriginalButtonCount,
                DeviceModel.ORIGINAL_V2 => DeviceConstants.OriginalButtonCount,
                _ => 0,
            };

            this.Columns = model switch
            {
                DeviceModel.XL => DeviceConstants.XLColumns,
                DeviceModel.MINI => DeviceConstants.MiniColumns,
                DeviceModel.ORIGINAL => DeviceConstants.OriginalColumns,
                DeviceModel.ORIGINAL_V2 => DeviceConstants.OriginalColumns,
                _ => 0,
            };

            this.Rows = model switch
            {
                DeviceModel.XL => DeviceConstants.XLRows,
                DeviceModel.MINI => DeviceConstants.MiniRows,
                DeviceModel.ORIGINAL => DeviceConstants.OriginalRows,
                DeviceModel.ORIGINAL_V2 => DeviceConstants.OriginalRows,
                _ => 0,
            };

            this.ButtonSize = model switch
            {
                DeviceModel.XL => DeviceConstants.XLButtonSize,
                DeviceModel.MINI => DeviceConstants.UniversalButtonSize,
                DeviceModel.ORIGINAL => DeviceConstants.UniversalButtonSize,
                DeviceModel.ORIGINAL_V2 => DeviceConstants.UniversalButtonSize,
                _ => 0,
            };

            longPressTimer.Elapsed += LongPressTimer_Elapsed;
            longPressTimer.Interval = Program.LongPressDelay;
        }

        /// <summary>
        /// Delegate responsible for handling Stream Deck button presses.
        /// </summary>
        /// <param name="source">The device where the button was pressed.</param>
        /// <param name="e">Information on the button press.</param>
        public delegate void ReceivedButtonPressHandler(object source, ButtonPressEventArgs e);

        private int pressedButtonId = 0;

        private System.Timers.Timer longPressTimer = new System.Timers.Timer();

        private bool longPress = false;

        /// <summary>
        /// Button press event handler.
        /// </summary>
        public event ReceivedButtonPressHandler OnButtonPress;

        public event EventHandler OnStreamClosed;

        public string SerialNumber { get; private set; }


        /// <summary>
        /// Gets the vendor ID.
        /// </summary>
        public int VId { get; private set; }

        /// <summary>
        /// Gets the product ID.
        /// </summary>
        public int PId { get; private set; }

        /// <summary>
        /// Gets the USB HID device path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the USB HID device name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the Stream Deck device model.
        /// </summary>
        public DeviceModel Model { get; private set; }

        /// <summary>
        /// Gets the number of buttons on the connected Stream Deck device.
        /// </summary>
        public int ButtonCount { get; }

        public int Columns { get; }

        public int Rows { get; }

        public int ButtonSize { get; }

        private HidDevice UnderlyingDevice { get; }

        private HidStream UnderlyingInputStream { get; set; }

        /// <summary>
        /// Initialize the device and start reading the input stream.
        /// </summary>
        public void InitializeDevice()
        {
            try
            {
                this.UnderlyingInputStream = this.UnderlyingDevice.Open();
                this.UnderlyingInputStream.ReadTimeout = Timeout.Infinite;
                this.UnderlyingInputStream.BeginRead(this.keyPressBuffer, 0, this.keyPressBuffer.Length, this.KeyPressCallback, null);
                this.UnderlyingInputStream.Closed += UnderlyingInputStream_Closed;
            } catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize device: {ex.Message}");
            }
        }

        private void UnderlyingInputStream_Closed(object sender, EventArgs e)
        {
            if (this.OnStreamClosed != null)
            {
                this.OnStreamClosed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Open the underlying Stream Deck device.
        /// </summary>
        /// <returns>HID stream that can be read or written to.</returns>
        public HidStream Open()
        {
            return this.UnderlyingDevice.Open();
        }

        public void Close()
        {
            if (this.UnderlyingInputStream != null)
            {
                this.UnderlyingInputStream.Dispose();
            }
        }
        /// <summary>
        /// Clear the contents of the Stream Deck buttons.
        /// </summary>
        public void ClearPanel()
        {
            for (int i = 0; i < this.ButtonCount; i++)
            {
                // TODO: Need to replace this with device-specific logic
                // since not every device is 96x96.
                this.SetKey(i, DeviceConstants.XLDefaultBlackButton);
            }
        }

        /// <summary>
        /// Sets the brightness of the Stream Deck device display.
        /// </summary>
        /// <param name="percentage">Percentage, from 0 to 100, to which brightness should be set. Any values larger than 100 will be set to 100.</param>
        public void SetBrightness(byte percentage)
        {
            try
            {
                if (percentage > 100)
                {
                    percentage = 100;
                }

                var brightnessRequest = new byte[]
                {
                0x03, 0x08, percentage, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                };

                using var stream = this.Open();
                stream.SetFeature(brightnessRequest);
            }
            catch (Exception ex)
            {
                return;
            }
        }


        /// <summary>
        /// Sets the content of a key on a Stream Deck device.
        /// </summary>
        /// <param name="keyId">Numberic ID of the key that needs to be set.</param>
        /// <param name="image">Binary content (JPEG) of the image that needs to be set on the key. The image will be resized to match the expectations of the connected device.</param>
        /// <returns>True if succesful, false if not.</returns>
        public bool SetKey(int keyId, byte[] image)
        {
            var content = image ?? DeviceConstants.XLDefaultBlackButton;

            var iteration = 0;
            var remainingBytes = content.Length;

            try
            {
                using (var stream = this.Open())
                {
                    while (remainingBytes > 0)
                    {
                        var sliceLength = Math.Min(remainingBytes, ImageReportPayloadLength);
                        var bytesSent = iteration * ImageReportPayloadLength;

                        byte finalizer = sliceLength == remainingBytes ? (byte)1 : (byte)0;

                        // These components are nothing else but UInt16 low-endian
                        // representations of the length of the image payload, and iteration.
                        var bitmaskedLength = (byte)(sliceLength & 0xFF);
                        var shiftedLength = (byte)(sliceLength >> ImageReportHeaderLength);
                        var bitmaskedIteration = (byte)(iteration & 0xFF);
                        var shiftedIteration = (byte)(iteration >> ImageReportHeaderLength);

                        // TODO: This is different for different device classes, so I will need
                        // to make sure that I adjust the header format.
                        byte[] header = new byte[] { 0x02, 0x07, (byte)keyId, finalizer, bitmaskedLength, shiftedLength, bitmaskedIteration, shiftedIteration };
                        var payload = header.Concat(new ArraySegment<byte>(content, bytesSent, sliceLength)).ToArray();
                        var padding = new byte[ImageReportLength - payload.Length];

                        var finalPayload = payload.Concat(padding).ToArray();

                        stream.Write(finalPayload);

                        remainingBytes -= sliceLength;
                        iteration++;
                    }
                }
            } catch (Exception ex)
            {
                return false;
            }
            

            return true;
        }

        private void KeyPressCallback(IAsyncResult result)
        {
            try
            {
                int bytesRead = this.UnderlyingInputStream.EndRead(result);

                var buttonData = new ArraySegment<byte>(this.keyPressBuffer, ButtonPressHeaderOffset, ButtonCount).ToArray();

                if (this.pressedButtonId == -1)
                {
                    if (this.OnButtonPress != null)
                    {
                        this.OnButtonPress(this.UnderlyingDevice, new ButtonPressEventArgs(pressedButtonId, ButtonEventKind.UP));
                    }
                }

                var pressedButton = Array.IndexOf(buttonData, (byte)1);
                if (pressedButton > -1)
                {
                    this.pressedButtonId = pressedButton;
                }

                var buttonKind = pressedButton != -1 ? ButtonEventKind.DOWN : ButtonEventKind.UP;

                switch (buttonKind)
                {
                    case ButtonEventKind.DOWN:
                        this.longPressTimer.Start();
                        break;
                    case ButtonEventKind.UP:
                        this.longPressTimer.Stop();
                        if (this.longPress == true)
                        {
                            buttonKind = ButtonEventKind.LONG_UP;
                        }
                        break;

                }

                if (this.OnButtonPress != null)
                {
                    this.OnButtonPress(this.UnderlyingDevice, new ButtonPressEventArgs(this.pressedButtonId, buttonKind));
                }


                Array.Clear(this.keyPressBuffer, 0, this.keyPressBuffer.Length);

                this.UnderlyingInputStream.BeginRead(this.keyPressBuffer, 0, this.keyPressBuffer.Length, this.KeyPressCallback, null);
            } catch (Exception ex)
            {
            }
        }
        
        private void LongPressTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.longPress = true;
            this.longPressTimer.Stop();

            if (this.OnButtonPress != null)
            {
                this.OnButtonPress(this.UnderlyingDevice, new ButtonPressEventArgs(this.pressedButtonId, ButtonEventKind.LONG_DOWN));
            }
        }

    }
}
