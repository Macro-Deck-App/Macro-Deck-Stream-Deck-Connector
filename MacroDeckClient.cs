using MacroDeck.StreamDeckConnector.Models;
using MacroDeck.StreamDeckConnector.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using MacroDeck.StreamDeckConnector.Enums;
using Websocket.Client;

namespace MacroDeck.StreamDeckConnector
{
    internal class MacroDeckClient
    {
        private readonly WebsocketClient _websocketClient;
        private readonly ConnectedDevice _connectedDevice;
        private readonly Timer _frameUpdateTimer;

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
            _websocketClient.MessageReceived.Subscribe(msg => Task.Run(() => HandleMessageAsync(msg.Text)));
            _websocketClient.Start();
            _ = SendAsync(new ConnectedMessageModel() { ClientId = _connectedDevice.SerialNumber });
            _connectedDevice.OnButtonPress += ConnectedDevice_OnButtonPress;
            _frameUpdateTimer = new Timer()
            {
                Interval = 40, // ~25 frames per second
                Enabled = true,
            };
            _frameUpdateTimer.Elapsed += FrameUpdateTimer_Elapsed;
            _frameUpdateTimer.Start();
        }

        internal void Close()
        {
            if (Closed) return;
            Closed = true;
            foreach (var button in _buttons)
            {
                button?.Dispose();
            }
            _websocketClient?.Dispose();
            try
            {
                _frameUpdateTimer.Stop();
                _frameUpdateTimer.Dispose();
            }
            catch
            {
                // ignored
            }

            _connectedDevice?.Close();
        }

        private async void ConnectedDevice_OnButtonPress(object source, ButtonPressEventArgs e)
        {
            var row = e.Id / _connectedDevice.Columns;
            var column = e.Id % _connectedDevice.Columns;
            var id = $"{row}_{column}";
            switch (e.Kind)
            {
                case ButtonEventKind.DOWN:
                    buttonPressed = true;
                    await SendAsync(new ButtonPressMessageModel() { Id = id });
                    break;
                case ButtonEventKind.UP:
                    buttonPressed = false;
                    await SendAsync(new ButtonReleaseMessageModel() { Id = id });
                    break;
                case ButtonEventKind.LONG_DOWN:
                    await SendAsync(new ButtonLongPressMessageModel() { Id = id });
                    break;
                case ButtonEventKind.LONG_UP:
                    buttonPressed = false;
                    await SendAsync(new ButtonLongPressReleaseMessageModel() { Id = id });
                    break;
            }
        }

        private void FrameUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateAllButtons();
        }

        private async Task HandleMessageAsync(string message)
        {
            var receivedMessageModel = BasicMessageModel.Deserialize(message);
            switch (receivedMessageModel.Method)
            {
                case MessageMethod.GET_CONFIG:
                    await SendAsync(new BasicMessageModel() { Method = MessageMethod.GET_BUTTONS });
                    break;
                case MessageMethod.GET_BUTTONS:
                    var buttonMessageModel = GetButtonsMessageModel.Deserialize(message);
                    foreach (var button in _buttons)
                    {
                        button?.Dispose();
                    }
                    _buttons = buttonMessageModel.Buttons;
                    UpdateAllButtons();
                    break;
                case MessageMethod.UPDATE_BUTTON:
                    var updateMessageModel = UpdateButtonMessageModel.Deserialize(message);
                    var actionButton = updateMessageModel.Buttons.FirstOrDefault();
                    if (actionButton == null) return;
                    var actionButtonOld = _buttons.Find(x => x.Row == actionButton.Row && x.Column == actionButton.Column);
                    if (actionButtonOld != null)
                    {
                        actionButtonOld.Dispose();
                        _buttons.Remove(actionButtonOld);
                    }
                    _buttons.Add(actionButton);
                    var id = actionButton.Row * _connectedDevice.Columns + actionButton.Column;
                    UpdateButton(id, actionButton);
                    break;
            }
        }

        private bool _buttonsUpdating;
        private void UpdateAllButtons()
        {
            if (_buttonsUpdating) return;
            _buttonsUpdating = true;
            for (var row = 0; row < _connectedDevice?.Rows; row++)
            {
                for (var col = 0; col < _connectedDevice?.Columns; col++)
                {
                    var keyId = row * _connectedDevice.Columns + col;
                    var actionButton = _buttons.ToArray().FirstOrDefault(x => x != null && x.Column == col && x.Row == row);
                    UpdateButton(keyId, actionButton);
                }
            }
            _buttonsUpdating = false;
        }

        private void UpdateButton(int id, ActionButtonModel? actionButton)
        {
            if (actionButton != null)
            {
                actionButton.FrameTick();
                var frame = actionButton.GetCurrentFrame(_connectedDevice.ButtonSize);
                if (frame == null) return;
                _connectedDevice?.SetKey(id, frame);
            }
            else
            {
                _connectedDevice?.SetKey(id, EmptyButtonImageGenerator.GetEmptyButton(_connectedDevice.ButtonSize));
            }
        }

        private async Task SendAsync(ISerializableModel messageModel)
        {
            if (_websocketClient == null) return;
            var message = messageModel.Serialize();
            await Task.Run(() => _websocketClient?.Send(message));
        }
    }
}
