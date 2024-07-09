using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.DataTypes.Internal;
using MacroDeck.StreamDeckConnector.Parsers;
using MacroDeck.StreamDeckConnector.Setup;
using Microsoft.Extensions.Hosting;
using Serilog;
using StreamDeckSharp;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector.HostedServices;

public class UsbHostedService : IHostedService
{
    private readonly ILogger _logger = Log.ForContext<UsbHostedService>();
    
    private readonly IUsbEventWatcher _usbEventWatcher;

    private readonly Dictionary<string, MacroDeckClient> _connectedClients = new();

    public UsbHostedService(
        IUsbEventWatcher usbEventWatcher)
    {
        _usbEventWatcher = usbEventWatcher;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _usbEventWatcher.UsbDeviceRemoved += UsbEventWatcherOnUsbDeviceRemoved;
        _usbEventWatcher.UsbDeviceAdded += UsbEventWatcherOnUsbDeviceAdded;
        Task.Run(async () => await Initialize(), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _usbEventWatcher.UsbDeviceRemoved -= UsbEventWatcherOnUsbDeviceRemoved;
        _usbEventWatcher.UsbDeviceAdded -= UsbEventWatcherOnUsbDeviceAdded;
        return Task.CompletedTask;
    }

    private async Task Initialize()
    {
        var connectedDevices = StreamDeck.EnumerateDevices();
        foreach (var device in connectedDevices)
        {
            _logger.Information("Found {DeviceDeviceName}@{DeviceDevicePath}", device.DeviceName, device.DevicePath);
            var connectedDevice = new ConnectedDevice(device.DevicePath, StartParameters.Instance.LongPressDelay);
            await ConnectDevice(connectedDevice);
        }
    }

    private async void UsbEventWatcherOnUsbDeviceAdded(object? sender, UsbDevice device)
    {
        if (_connectedClients.ContainsKey(device.SerialNumber))
        {
            return;
        }
            
        _logger.Information("{DeviceSerialNumber} added", device.SerialNumber);
        _logger.Information("Vendor ID: {VendorId}",
            int.Parse(device.VendorID, System.Globalization.NumberStyles.HexNumber));
        _logger.Information("Product ID: {ProductId}",
            int.Parse(device.ProductID, System.Globalization.NumberStyles.HexNumber));
        _logger.Information("Serial Number: {SerialNumber}", device.SerialNumber);
        _logger.Information("Description: {ProductDescription}", device.ProductDescription);
        try
        {
            var serialNumber = SerialNumberParser.SerialNumberFromDevicePath(device.DeviceSystemPath);
            var streamDeckRefHandle = StreamDeck.EnumerateDevices()
                .FirstOrDefault(d => SerialNumberParser.SerialNumberFromDevicePath(d.DevicePath) == serialNumber);
            if (streamDeckRefHandle == null)
            {
                return;
            }

            var connectedDevice =
                new ConnectedDevice(streamDeckRefHandle.DevicePath, StartParameters.Instance.LongPressDelay);
            await ConnectDevice(connectedDevice);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse serial number: {ex.Message}");
        }
    }

    private void UsbEventWatcherOnUsbDeviceRemoved(object? sender, UsbDevice device)
    {
        if (!_connectedClients.ContainsKey(device.SerialNumber))
        {
            return;
        }
            
        Console.WriteLine($"{device.SerialNumber} removed");
        _connectedClients[device.SerialNumber].Close();
        _connectedClients.Remove(device.SerialNumber);
    }

    private async ValueTask ConnectDevice(ConnectedDevice connectedDevice)
    {
        if (_connectedClients.TryGetValue(connectedDevice.SerialNumber, out var client))
        {
            client.Close();
            _connectedClients.Remove(connectedDevice.SerialNumber);
        }

        var protocol = StartParameters.Instance.WebSocketSecure ? "wss://" : "ws://";
        var uri = new Uri($"{protocol}{StartParameters.Instance.Host}");
        
        client = new MacroDeckClient(uri, connectedDevice);
        _connectedClients.Add(connectedDevice.SerialNumber, client);

        try
        {
            await client.Start();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start client");
        }
    }
}