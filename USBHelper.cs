using System;
using System.Collections.Generic;
using System.Linq;
using MacroDeck.StreamDeckConnector.Parsers;
using StreamDeckSharp;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector
{
    internal class USBHelper
    {
        private static Dictionary<string, MacroDeckClient> connectedClients = new Dictionary<string, MacroDeckClient>();

        public static void Initialize()
        {
            IUsbEventWatcher usbEventWatcher = new UsbEventWatcher(includeTTY: true);

            usbEventWatcher.UsbDeviceRemoved += (_, device) =>
            {
                if (!connectedClients.ContainsKey(device.SerialNumber)) return;
                Console.WriteLine($"{device.SerialNumber} removed");
                connectedClients[device.SerialNumber].Close();
                connectedClients.Remove(device.SerialNumber);
            };

            usbEventWatcher.UsbDeviceAdded += (_, device) =>
            {
                if (connectedClients.ContainsKey(device.SerialNumber)) return;
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
                    ConnectDevice(connectedDevice);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse serial number: {ex.Message}");
                }

            };

            var devices = StreamDeck.EnumerateDevices();
            foreach (var device in devices)
            {
                try
                {
                    var connectedDevice = new ConnectedDevice(device.DevicePath);
                    ConnectDevice(connectedDevice);
                } catch {}
            }
        }

        private static void ConnectDevice(ConnectedDevice connectedDevice)
        {
            if (connectedClients.ContainsKey(connectedDevice.SerialNumber))
            {
                connectedClients[connectedDevice.SerialNumber].Close();
                connectedClients.Remove(connectedDevice.SerialNumber);
            }
            var client = new MacroDeckClient(new Uri($"ws://{Program.Host}"), connectedDevice);
            connectedClients.Add(connectedDevice.SerialNumber, client);
        }
    }
}
