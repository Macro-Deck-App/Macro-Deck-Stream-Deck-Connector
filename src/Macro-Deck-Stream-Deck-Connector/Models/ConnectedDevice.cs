using System;
using System.Linq;
using HidSharp;
using MacroDeck.StreamDeckConnector.Enums;
using MacroDeck.StreamDeckConnector.Setup;
using Microsoft.Extensions.DependencyInjection;
using OpenMacroBoard.SDK;
using StreamDeckSharp;
using Timer = System.Timers.Timer;

namespace MacroDeck.StreamDeckConnector.Models;

public class ConnectedDevice
{
    public event EventHandler<ButtonPressEventArgs>? OnButtonPress;
    
    private readonly IMacroBoard? _streamDeck;
    private readonly Timer _longPressTimer = new();
    private bool _longPress;
    
    private int PressedButtonId { get; set; }
    public string SerialNumber { get; }
    private DeviceModel Model { get; }
    private int ButtonCount { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int ButtonSize { get; }
    public bool Closed { get; private set; }


    public ConnectedDevice(string path, IServiceScope scope)
    {
        _longPressTimer.Elapsed += LongPressTimer_Elapsed;
        _longPressTimer.Interval = scope.ServiceProvider.GetRequiredService<StartParameters>().LongPressDelay;

        try
        {
            _streamDeck = StreamDeck.OpenDevice(path)
                .WithButtonPressEffect();
            
            var hidDevice = DeviceList.Local.GetHidDevices()
                .First(d => d.DevicePath == path);
            
            SerialNumber = hidDevice?.GetSerialNumber() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot open {path}: {ex.Message}");
            throw;
        }

        ButtonCount = _streamDeck.Keys.Count;
        Model = ButtonCount switch
        {
            32 => DeviceModel.XL,
            6 => DeviceModel.MINI,
            15 => DeviceModel.ORIGINAL,
            _ => DeviceModel.ORIGINAL,
        };
        Columns = Model switch
        {
            DeviceModel.XL => DeviceConstants.XLColumns,
            DeviceModel.MINI => DeviceConstants.MiniColumns,
            DeviceModel.ORIGINAL => DeviceConstants.OriginalColumns,
            _ => 0,
        };
        Rows = Model switch
        {
            DeviceModel.XL => DeviceConstants.XLRows,
            DeviceModel.MINI => DeviceConstants.MiniRows,
            DeviceModel.ORIGINAL => DeviceConstants.OriginalRows,
            _ => 0,
        };
        ButtonSize = Model switch
        {
            DeviceModel.XL => DeviceConstants.XLButtonSize,
            DeviceModel.MINI => DeviceConstants.UniversalButtonSize,
            DeviceModel.ORIGINAL => DeviceConstants.UniversalButtonSize,
            _ => DeviceConstants.UniversalButtonSize,
        };
        _streamDeck.KeyStateChanged += StreamDeck_KeyStateChanged;
    }

    private void StreamDeck_KeyStateChanged(object? sender, KeyEventArgs e)
    {
        if (e.Key > -1)
        {
            PressedButtonId = e.Key;
        }

        var buttonKind = ButtonEventKind.DOWN;

        if (e.IsDown)
        {
            _longPressTimer.Start();
        }
        else
        {
            OnButtonPress?.Invoke(sender, new ButtonPressEventArgs(PressedButtonId, ButtonEventKind.UP));
            if (_longPress)
            {
                buttonKind = ButtonEventKind.LONG_UP;
            }
        }

        OnButtonPress?.Invoke(sender, new ButtonPressEventArgs(PressedButtonId, buttonKind));
    }

    private void LongPressTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _longPress = true;
        _longPressTimer.Stop();
        OnButtonPress?.Invoke(this, new ButtonPressEventArgs(PressedButtonId, ButtonEventKind.LONG_DOWN));
    }

    public void Close()
    {
        Closed = true;
        _longPressTimer.Dispose();
        _streamDeck?.Dispose();
    }

    public void SetKey(int keyId, KeyBitmap bitmap)
    {
        try
        {
            if (Closed || _streamDeck?.IsConnected is false)
            {
                return;
            }
            
            _streamDeck?.SetKeyBitmap(keyId, bitmap);
        }
        catch
        {
        }
    }
}