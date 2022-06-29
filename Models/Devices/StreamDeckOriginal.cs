using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeckSurf.SDK.Models.Devices
{
    /// <summary>
    /// Implementation for a Stream Deck Original/v2 connected device.
    /// </summary>
    public class StreamDeckOriginal : ConnectedDevice
    {
        public StreamDeckOriginal(int vid, int pid, string path, string name, DeviceModel model, string serialNumber) : base(vid, pid, path, name, model, serialNumber)
        {
        }
    }
}
