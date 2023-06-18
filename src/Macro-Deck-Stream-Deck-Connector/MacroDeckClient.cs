using MacroDeck.StreamDeckConnector.Models;
using MacroDeck.StreamDeckConnector.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.Enums;
using Websocket.Client;

namespace MacroDeck.StreamDeckConnector;

internal class MacroDeckClient
{
    private readonly WebsocketClient _websocketClient;
    private readonly ConnectedDevice _connectedDevice;

    private List<ActionButtonModel> _buttons = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly SemaphoreSlim _frameUpdateSemaphoreSlim = new(1);

    private bool Closed { get; set; }

    public MacroDeckClient(Uri uri, ConnectedDevice connectedDevice)
    {
        _connectedDevice = connectedDevice;
        _websocketClient = new WebsocketClient(uri)
        {
            ReconnectTimeout = null,
        };
    }

    internal async ValueTask Start()
    {
        _websocketClient.MessageReceived.Subscribe(msg => Task.Run(async () => await HandleMessageAsync(msg.Text)));
        await _websocketClient.Start();
        await SendAsync(new ConnectedMessageModel
        {
            ClientId = _connectedDevice.SerialNumber
        });
        _connectedDevice.OnButtonPress += ConnectedDevice_OnButtonPress;
        
        await Task.Run(async () => await DoFrameUpdate(_cancellationTokenSource.Token));
    }

    internal void Close()
    {
        if (Closed)
        {
            return;
        }
        Closed = true;
            
        _cancellationTokenSource.Cancel();
        foreach (var button in _buttons)
        {
            button.Dispose();
        }
            
        _websocketClient.Dispose();
        _connectedDevice.Close();
    }

    private async ValueTask DoFrameUpdate(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await UpdateAllButtons();
            await Task.Delay(16, cancellationToken);
        }
    }

    private async void ConnectedDevice_OnButtonPress(object source, ButtonPressEventArgs e)
    {
        var row = e.Id / _connectedDevice.Columns;
        var column = e.Id % _connectedDevice.Columns;
        var id = $"{row}_{column}";
        switch (e.Kind)
        {
            case ButtonEventKind.DOWN:
                await SendAsync(new ButtonPressMessageModel { Id = id });
                break;
            case ButtonEventKind.UP:
                await SendAsync(new ButtonReleaseMessageModel { Id = id });
                break;
            case ButtonEventKind.LONG_DOWN:
                await SendAsync(new ButtonLongPressMessageModel { Id = id });
                break;
            case ButtonEventKind.LONG_UP:
                await SendAsync(new ButtonLongPressReleaseMessageModel { Id = id });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async ValueTask HandleMessageAsync(string message)
    {
        var receivedMessageModel = BasicMessageModel.Deserialize(message);
        switch (receivedMessageModel.Method)
        {
            case MessageMethod.GET_CONFIG:
                await SendAsync(new BasicMessageModel { Method = MessageMethod.GET_BUTTONS });
                break;
            case MessageMethod.GET_BUTTONS:
                var buttonMessageModel = GetButtonsMessageModel.Deserialize(message);
                _buttons = buttonMessageModel.Buttons;
                await UpdateAllButtons();
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

    private async ValueTask UpdateAllButtons()
    {
        await _frameUpdateSemaphoreSlim.WaitAsync();
            
        var buttonsByPosition = _buttons
            .ToDictionary(x => new Tuple<int, int>(x.Row, x.Column));

        for (var row = 0; row < _connectedDevice.Rows; row++)
        {
            for (var col = 0; col < _connectedDevice.Columns; col++)
            {
                var keyId = row * _connectedDevice.Columns + col;
                buttonsByPosition.TryGetValue(new Tuple<int, int>(row, col), out var actionButton);
                UpdateButton(keyId, actionButton);  
            }
        }

        _frameUpdateSemaphoreSlim.Release();
    }

    private void UpdateButton(int id, ActionButtonModel? actionButton)
    {
        if (_connectedDevice.Closed)
        {
            return;
        }
            
        if (actionButton != null)
        {
            actionButton.FrameTick();
            var frame = actionButton.GetCurrentFrame(_connectedDevice.ButtonSize);
            if (frame == null) return;
            _connectedDevice.SetKey(id, frame);
        }
        else
        {
            _connectedDevice.SetKey(id, EmptyButtonImageGenerator.GetEmptyButton(_connectedDevice.ButtonSize));
        }
    }

    private async ValueTask SendAsync(ISerializableModel messageModel)
    {
        if (!_websocketClient.IsRunning)
        {
            return;
        }
            
        var message = messageModel.Serialize();
        await _websocketClient.SendInstant(message);
    }
}