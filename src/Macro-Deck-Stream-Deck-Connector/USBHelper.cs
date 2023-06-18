using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.Models;
using MacroDeck.StreamDeckConnector.Parsers;
using StreamDeckSharp;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector;

internal class USBHelper
{
    private static readonly Dictionary<string, MacroDeckClient> ConnectedClients = new();

    public static async ValueTask Initialize()
    {
        IUsbEventWatcher usbEventWatcher = new UsbEventWatcher(includeTTY: true);

        usbEventWatcher.UsbDeviceRemoved += (_, device) =>
        {
            if (!ConnectedClients.ContainsKey(device.SerialNumber)) return;
            Console.WriteLine($"{device.SerialNumber} removed");
            ConnectedClients[device.SerialNumber].Close();
            ConnectedClients.Remove(device.SerialNumber);
        };

        usbEventWatcher.UsbDeviceAdded += async (_, device) =>
        {
            if (ConnectedClients.ContainsKey(device.SerialNumber)) return;
            Console.WriteLine($"{device.SerialNumber} added");
            Console.WriteLine("Vendor ID: " + int.Parse(device.VendorID, System.Globalization.NumberStyles.HexNumber));
            Console.WriteLine("Product ID: " + int.Parse(device.ProductID, System.Globalization.NumberStyles.HexNumber));
            Console.WriteLine("Serial Number: " + device.SerialNumber);
            Console.WriteLine("Description: " + device.ProductDescription);
            try
            {
                var serialNumber = SerialNumberParser.SerialNumberFromDevicePath(device.DeviceSystemPath);
                var streamDeckRefHandle = StreamDeck.EnumerateDevices().FirstOrDefault(d =>
                    SerialNumberParser.SerialNumberFromDevicePath(d.DevicePath) == serialNumber);
                    
                if (streamDeckRefHandle == null) return;
                var connectedDevice = new ConnectedDevice(streamDeckRefHandle.DevicePath);
                await ConnectDevice(connectedDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse serial number: {ex.Message}");
            }

        };

        var devices = StreamDeck.EnumerateDevices();
        foreach (var device in devices)
        {
            Console.WriteLine($"Found {device.DeviceName} @ {device.DevicePath}");
            try
            {
                var connectedDevice = new ConnectedDevice(device.DevicePath);
                await ConnectDevice(connectedDevice);
            } catch {}
        }
    }

    private static async ValueTask ConnectDevice(ConnectedDevice connectedDevice)
    {
        if (ConnectedClients.TryGetValue(connectedDevice.SerialNumber, out var value))
        {
            value.Close();
            ConnectedClients.Remove(connectedDevice.SerialNumber);
        }
        var client = new MacroDeckClient(new Uri($"ws://{Program.Host}"), connectedDevice);
        await client.Start();
        ConnectedClients.Add(connectedDevice.SerialNumber, client);
    }
}