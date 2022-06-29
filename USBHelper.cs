using DeckSurf.SDK.Core;
using DeckSurf.SDK.Models;
using MacroDeck.StreamDeckConnector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Usb.Events;

namespace MacroDeck.StreamDeckConnector
{
    internal class USBHelper
    {
        private static Dictionary<string, MacroDeckClient> connectedClients = new Dictionary<string, MacroDeckClient>();

        public static void Initialize()
        {
            IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();

            usbEventWatcher.UsbDeviceRemoved += (_, device) =>
            {
                if (connectedClients.ContainsKey(device.SerialNumber))
                {
                    connectedClients[device.SerialNumber].Dispose();
                    connectedClients.Remove(device.SerialNumber);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            };

            usbEventWatcher.UsbDeviceAdded += (_, device) =>
            {
                if (!IsSupported(device)) return;
                if (connectedClients.ContainsKey(device.SerialNumber)) return;
                Console.WriteLine("Vendor ID: " + Int32.Parse(device.VendorID, System.Globalization.NumberStyles.HexNumber));
                Console.WriteLine("Product ID: " + Int32.Parse(device.ProductID, System.Globalization.NumberStyles.HexNumber));
                Console.WriteLine("Serial Number: " + device.SerialNumber);
                Console.WriteLine("Description: " + device.ProductDescription);

                var connectedDevice = DeviceManager.GetDeviceList().Where(x => x.SerialNumber == device.SerialNumber).FirstOrDefault();
                if (connectedDevice == null) return;
                ConnectDevice(connectedDevice);
            };


            var devices = DeviceManager.GetDeviceList();
            foreach (var connectedDevice in devices)
            {
                ConnectDevice(connectedDevice);
            }
        }

        private static void ConnectDevice(ConnectedDevice connectedDevice)
        {
            if (connectedClients.ContainsKey(connectedDevice.SerialNumber))
            {
                connectedClients[connectedDevice.SerialNumber].Close();
                connectedClients.Remove(connectedDevice.SerialNumber);
            }
            Console.WriteLine($"Connecting {connectedDevice.Model} {connectedDevice.PId} {connectedDevice.SerialNumber}");
            var client = new MacroDeckClient(new Uri($"ws://{Program.Host}"), connectedDevice);
            connectedClients.Add(connectedDevice.SerialNumber, client);
        }

        private static bool IsSupported(UsbDevice device)
        {
            int vendorId = Int32.Parse(device.VendorID, System.Globalization.NumberStyles.HexNumber);
            int productId = Int32.Parse(device.ProductID, System.Globalization.NumberStyles.HexNumber);
            return device.ProductDescription == "USB Input Device" && DeviceManager.IsSupported(vendorId, productId);
        }
    }
}
