// Copyright (c) Den Delimarsky
// Den Delimarsky licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DeckSurf.SDK.Models;
using DeckSurf.SDK.Models.Devices;
using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeckSurf.SDK.Core
{
    /// <summary>
    /// Class used to manage connected Stream Deck devices.
    /// </summary>
    public class DeviceManager
    {
        public static readonly int SupportedVid = 4057;

        /// <summary>
        /// Return a list of connected Stream Deck devices supported by DeckSurf.
        /// </summary>
        /// <returns>Enumerable containing a list of supported devices.</returns>
        public static IEnumerable<ConnectedDevice> GetDeviceList()
        {
            var connectedDevices = new List<ConnectedDevice>();
            var deviceList = DeviceList.Local.GetHidDevices();

            foreach (var device in deviceList.Where(x => x.VendorID == SupportedVid))
            {
                bool supported = IsSupported(device.VendorID, device.ProductID);

                Console.WriteLine("Vendor ID: " + device.VendorID);
                Console.WriteLine("Product ID: " + device.ProductID);
                Console.WriteLine("Serial Number: " + device.GetSerialNumber());

                if (supported)
                {
                    switch ((DeviceModel)device.ProductID)
                    {
                        case DeviceModel.XL:
                            connectedDevices.Add(new StreamDeckXL(device.VendorID, device.ProductID, device.DevicePath, device.GetFriendlyName(), (DeviceModel)device.ProductID, device.GetSerialNumber()));
                            break;
                        case DeviceModel.MINI:
                            connectedDevices.Add(new StreamDeckMini(device.VendorID, device.ProductID, device.DevicePath, device.GetFriendlyName(), (DeviceModel)device.ProductID, device.GetSerialNumber()));
                            break;
                        case DeviceModel.ORIGINAL:
                        case DeviceModel.ORIGINAL_V2:
                            connectedDevices.Add(new StreamDeckOriginal(device.VendorID, device.ProductID, device.DevicePath, device.GetFriendlyName(), (DeviceModel)device.ProductID, device.GetSerialNumber()));
                            break;
                        default:
                            break;
                    }
                }
            }

            return connectedDevices;
        }

        /// <summary>
        /// Determines whether a given vendor ID (VID) and product ID (PID) are supported by the SDK. VID and PID should be representing a Stream Deck device.
        /// </summary>
        /// <param name="vid">Device VID.</param>
        /// <param name="pid">Device PID.</param>
        /// <returns>True if device is supported, false if not.</returns>
        public static bool IsSupported(int vid, int pid)
        {
            if (vid == SupportedVid && Enum.IsDefined(typeof(DeviceModel), (byte)pid))
            {
                return true;
            }

            return false;
        }
    }
}
