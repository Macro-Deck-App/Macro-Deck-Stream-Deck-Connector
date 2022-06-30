using DeckSurf.SDK.Core;
using DeckSurf.SDK.Models;
using MacroDeck.StreamDeckConnector.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Websocket.Client;

namespace MacroDeck.StreamDeckConnector
{
    internal class MacroDeckClient
    {
        private IntPtr _bufferPtr;
        private int BUFFER_SIZE = 1024 * 1024;
        private bool _disposed = false;

        public MacroDeckClient()
        {
            _bufferPtr = Marshal.AllocHGlobal(BUFFER_SIZE);
        }

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

            Close();

            Marshal.FreeHGlobal(_bufferPtr);
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MacroDeckClient()
        {
            Dispose(false);
        }


        private WebsocketClient _websocketClient;

        private ConnectedDevice _connectedDevice;

        private Timer _frameUpdateTimer;

        private List<ActionButtonModel> _buttons = new List<ActionButtonModel>();

        public bool Closed { get; private set; } = false;

        private bool buttonPressed = false;

        public MacroDeckClient(Uri uri, ConnectedDevice connectedDevice)
        {
            _connectedDevice = connectedDevice;
            _websocketClient = new WebsocketClient(uri)
            {
                ReconnectTimeout = null,
            };
            _websocketClient.MessageReceived.Subscribe(msg => HandleMessage(msg.Text));
            _websocketClient.Start();
            SendAsync(new ConnectedMessageModel() { ClientId = _connectedDevice.SerialNumber });
            _connectedDevice.OnButtonPress += ConnectedDevice_OnButtonPress;
            _connectedDevice.InitializeDevice();
            _connectedDevice.OnStreamClosed += _connectedDevice_OnStreamClosed;
            _frameUpdateTimer = new Timer()
            {
                Interval = 66, // ~15 frames per second
                Enabled = true,
            };
            _frameUpdateTimer.Elapsed += FrameUpdateTimer_Elapsed;
            _frameUpdateTimer.Start();
        }

        internal void Close()
        {
            if (Closed) return;
            Closed = true;
            foreach (var button in this._buttons)
            {
                button?.Dispose();
            }
            if (_websocketClient != null)
            {
                _websocketClient.Dispose();
            }
            if (_frameUpdateTimer != null)
            {
                _frameUpdateTimer.Stop();
                _frameUpdateTimer.Dispose();
            }
            if (_connectedDevice != null)
            {
                _connectedDevice.Close();
            }
        }

        private void _connectedDevice_OnStreamClosed(object sender, EventArgs e)
        {
            Close();
        }

        private void ConnectedDevice_OnButtonPress(object source, ButtonPressEventArgs e)
        {
            int row = e.Id / this._connectedDevice.Columns;
            int column = e.Id % this._connectedDevice.Columns;
            string id = $"{row}_{column}";
            switch (e.Kind)
            {
                case ButtonEventKind.DOWN:
                    this.buttonPressed = true;
                    SendAsync(new ButtonPressMessageModel() { Id = id });
                    break;
                case ButtonEventKind.UP:
                    this.buttonPressed = false;
                    SendAsync(new ButtonReleaseMessageModel() { Id = id });
                    break;
                case ButtonEventKind.LONG_DOWN:
                    SendAsync(new ButtonLongPressMessageModel() { Id = id });
                    break;
                case ButtonEventKind.LONG_UP:
                    this.buttonPressed = false;
                    SendAsync(new ButtonLongPressReleaseMessageModel() { Id = id });
                    break;
            }
        }

        private void FrameUpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            UpdateAllButtons();
        }

        private void HandleMessage(string message)
        {
            var receivedMessageModel = BasicMessageModel.Deserialize(message);
            switch (receivedMessageModel.Method)
            {
                case Enums.MessageMethod.GET_CONFIG:
                    SendAsync(new BasicMessageModel() { Method = Enums.MessageMethod.GET_BUTTONS });
                    break;
                case Enums.MessageMethod.GET_BUTTONS:
                    var buttonMessageModel = GetButtonsMessageModel.Deserialize(message);
                    foreach (var button in this._buttons)
                    {
                        button?.Dispose();
                    }
                    this._buttons = buttonMessageModel.Buttons;
                    UpdateAllButtons();
                    break;
                case Enums.MessageMethod.UPDATE_BUTTON:
                    var updateMessageModel = UpdateButtonMessageModel.Deserialize(message);
                    var actionButton = updateMessageModel.Buttons[0];
                    if (actionButton == null) return;
                    var actionButtonOld = this._buttons.Find(x => x.Row == actionButton.Row && x.Column == actionButton.Column);
                    if (actionButtonOld != null)
                    {
                        actionButtonOld.Dispose();
                        this._buttons.Remove(actionButtonOld);
                    }
                    this._buttons.Add(actionButton);
                    int id = actionButton.Row * this._connectedDevice.Columns + actionButton.Column;
                    UpdateButton(id, actionButton);
                    break;

            }
        }

        private void UpdateAllButtons()
        {
            if (_connectedDevice == null) return;
            int buttonIndex = 0;
            for (int row = 0; row < _connectedDevice?.Rows; row++)
            {
                for (int col = 0; col < _connectedDevice?.Columns; col++)
                {
                    var actionButton = this._buttons.Find(x => x.Column == col && x.Row == row);
                    UpdateButton(buttonIndex, actionButton);
                    buttonIndex++;
                }
            }
        }

        private void UpdateButton(int id, ActionButtonModel? actionButton)
        {
            if (_connectedDevice == null) return;
            if (actionButton != null)
            {
                actionButton.FrameTick();
                var frame = actionButton.GetCurrentFrame(this._connectedDevice.ButtonSize, this.buttonPressed && this._connectedDevice?.PressedButtonId == id);
                if (frame == null) return;
                this._connectedDevice?.SetKey(id, frame);
            }
            else
            {
                this._connectedDevice?.SetKey(id, DeviceConstants.XLDefaultBlackButton);
            }
        }

        public void SendAsync(IBaseMessageModel messageModel)
        {
            if (_websocketClient == null) return;

            Task.Run(() =>
            {
                string message = messageModel.Serialize();
                _websocketClient?.Send(message);
            });
        }
    }
}
