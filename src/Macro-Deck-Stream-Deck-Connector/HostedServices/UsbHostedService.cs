using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.Models;
using MacroDeck.StreamDeckConnector.Parsers;
using MacroDeck.StreamDeckConnector.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamDeckSharp;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector.HostedServices;

public class UsbHostedService : IHostedService
{
    private readonly IUsbEventWatcher _usbEventWatcher;
    private readonly StartParameters _startParameters;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly Dictionary<string, MacroDeckClient> _connectedClients = new();

    public UsbHostedService(
        IUsbEventWatcher usbEventWatcher,
        StartParameters startParameters,
        IServiceScopeFactory serviceScopeFactory)
    {
        _usbEventWatcher = usbEventWatcher;
        _startParameters = startParameters;
        _serviceScopeFactory = serviceScopeFactory;
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

    private async ValueTask Initialize()
    {
        var connectedDevices = StreamDeck.EnumerateDevices();
        foreach (var device in connectedDevices)
        {
            Console.WriteLine($"Found {device.DeviceName} @ {device.DevicePath}");
            var connectedDevice = new ConnectedDevice(device.DevicePath, _serviceScopeFactory.CreateScope());
            await ConnectDevice(connectedDevice);
        }
    }

    private async void UsbEventWatcherOnUsbDeviceAdded(object? sender, UsbDevice device)
    {
        if (_connectedClients.ContainsKey(device.SerialNumber))
        {
            return;
        }
            
        Console.WriteLine($"{device.SerialNumber} added");
        Console.WriteLine("Vendor ID: " + int.Parse(device.VendorID, System.Globalization.NumberStyles.HexNumber));
        Console.WriteLine("Product ID: " + int.Parse(device.ProductID, System.Globalization.NumberStyles.HexNumber));
        Console.WriteLine("Serial Number: " + device.SerialNumber);
        Console.WriteLine("Description: " + device.ProductDescription);
        try
        {
            var serialNumber = SerialNumberParser.SerialNumberFromDevicePath(device.DeviceSystemPath);
            var streamDeckRefHandle = StreamDeck.EnumerateDevices()
                .FirstOrDefault(d => SerialNumberParser.SerialNumberFromDevicePath(d.DevicePath) == serialNumber);
            if (streamDeckRefHandle == null)
            {
                return;
            }
            
            var connectedDevice = new ConnectedDevice(streamDeckRefHandle.DevicePath, _serviceScopeFactory.CreateScope());
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

        var protocol = _startParameters.WebSocketSecure ? "wss://" : "ws://";
        var uri = new Uri($"{protocol}{_startParameters.Host}");
        
        client = new MacroDeckClient(uri, connectedDevice);
        _connectedClients.Add(connectedDevice.SerialNumber, client);

        try
        {
            await client.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to start client");
        }
    }
}