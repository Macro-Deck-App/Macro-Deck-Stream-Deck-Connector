using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.DataTypes.Internal.Enums;
using MacroDeck.StreamDeckConnector.Events;
using MacroDeck.StreamDeckConnector.Extensions;
using MacroDeck.StreamDeckConnector.Models;
using MacroDeck.StreamDeckConnector.Utils;
using Websocket.Client;

namespace MacroDeck.StreamDeckConnector.DataTypes.Internal;

internal class MacroDeckClient
{
    private readonly WebsocketClient _websocketClient;
    private readonly ConnectedDevice _connectedDevice;

    private List<ActionButton> _buttons = new();
    
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private CancellationTokenSource? _renderCancellationTokenSource;

    private bool Closed { get; set; }

    public MacroDeckClient(Uri uri, ConnectedDevice connectedDevice)
    {
        _connectedDevice = connectedDevice;
        _websocketClient = new WebsocketClient(uri, WebSocketClientFactory.GetWebSocket())
        {
            ReconnectTimeout = null
        };
    }

    internal async Task Start()
    {
        _websocketClient.MessageReceived.Subscribe(msg => Task.Run(async () => await HandleMessageAsync(msg.Text)));
        
        await _websocketClient.Start();
        await SendAsync(new ConnectedMessageModel
        {
            ClientId = _connectedDevice.SerialNumber
        });
        _connectedDevice.OnButtonPress += ConnectedDevice_OnButtonPress;
        
        _ = Task.Run(async () => await DoFrameUpdate(_cancellationTokenSource.Token));
    }

    internal void Close()
    {
        if (Closed)
        {
            return;
        } 
        Closed = true;
            
        _cancellationTokenSource.Cancel();
        _websocketClient.Dispose();
        _connectedDevice.Close();
    }

    private async Task DoFrameUpdate(CancellationToken cancellationToken)
    {
        var debugStopwatch = new Stopwatch();
        var updateStopwatch = new Stopwatch();
        while (!cancellationToken.IsCancellationRequested)
        {
            debugStopwatch.Restart();
            updateStopwatch.Restart();
            UpdateAllButtons();
            updateStopwatch.Stop();
            var delay = 40 - updateStopwatch.ElapsedMilliseconds;
            if (delay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
            }
            debugStopwatch.Stop();
            var frameTime = debugStopwatch.Elapsed;
            //Log.Information("FrameTime: {Time}", frameTime.TotalMilliseconds);
            //Log.Information("FPS: {Time}", 1000 / frameTime.TotalMilliseconds);
        }
    }

    private async void ConnectedDevice_OnButtonPress(object? source, ButtonPressEventArgs e)
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

    private async ValueTask HandleMessageAsync(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
        
        var receivedMessageModel = BasicMessageModel.Deserialize(message);
        switch (receivedMessageModel.Method)
        {
            case MessageMethod.GET_CONFIG:
                await SendAsync(new BasicMessageModel { Method = MessageMethod.GET_BUTTONS });
                break;
            case MessageMethod.GET_BUTTONS:
                _renderCancellationTokenSource?.Cancel();
                _renderCancellationTokenSource = new CancellationTokenSource();
                var token = _renderCancellationTokenSource.Token;
                var buttonMessageModel = GetButtonsMessageModel.Deserialize(message);
                var receivedButtons = buttonMessageModel.Buttons.Select(x => x.ToActionButton()).ToList();
                var renderTasks = receivedButtons.Select(x => x.RenderButtonImage(token));
                try
                {
                    foreach (var renderTask in renderTasks)
                    {
                        _ = Task.Run(async () => await renderTask, token);
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                
                _buttons.Clear();
                _buttons.AddRange(receivedButtons);
                break;
            case MessageMethod.UPDATE_BUTTON:
                var updateMessageModel = UpdateButtonMessageModel.Deserialize(message);
                var v2ActionButton = updateMessageModel.Buttons.FirstOrDefault();
                if (v2ActionButton == null)
                {
                    return;
                }
                var actionButton = v2ActionButton.ToActionButton();
                await actionButton.RenderButtonImage(CancellationToken.None);
                var actionButtonOld =
                    _buttons.Find(x => x.Row == v2ActionButton.PositionY && x.Column == v2ActionButton.PositionX);
                if (actionButtonOld != null)
                {
                    _buttons.Remove(actionButtonOld);
                }
                var id = v2ActionButton.PositionY * _connectedDevice.Columns + v2ActionButton.PositionX;
                
                _buttons.Add(actionButton);
                UpdateButton(id, actionButton);
                break;
        }
    }

    private void UpdateAllButtons()
    {
        var bufferedButtons = _buttons.ToList();

        var buttonMaxCount = _connectedDevice.Rows * _connectedDevice.Columns;
        for (var index = 0; index < buttonMaxCount; index++)
        {
            var row = index / _connectedDevice.Columns;
            var column = index % _connectedDevice.Columns;
            var actionButton = bufferedButtons.SingleOrDefault(x => x.Row == row && x.Column == column);
            UpdateButton(index, actionButton);
        }
    }

    private void UpdateButton(int id, ActionButton? actionButton)
    {
        if (_connectedDevice.Closed)
        {
            return;
        }
            
        if (actionButton?.CurrentFrame is not null)
        {
            actionButton.Update();
            _connectedDevice.SetKey(id, actionButton.CurrentFrame.KeyBitmap);
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