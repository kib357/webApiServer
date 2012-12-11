using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BacNetApi
{
    public delegate void FoundDeviceEventHandler(uint deviceId);

    class DeviceFinder
    {
        private readonly BacNet _network;
        private readonly AutoResetEvent _waitForAddress = new AutoResetEvent(false);

        public static event FoundDeviceEventHandler FoundDeviceChangedEvent;

        internal void OnFoundDeviceEvent(uint deviceId)
        {
            FoundDeviceEventHandler handler = FoundDeviceChangedEvent;
            if (handler != null) handler(deviceId);
        }

        public DeviceFinder(BacNet network)
        {
            _network = network;
            _network.DeviceListChangedEvent += DeviceListChanged;
        }

        private void DeviceListChanged(List<BacNetDevice> changedDevices)
        {
            var dc = new BacnetDeviceComparer();
            changedDevices.Sort(dc);
            _network.WhoIs((ushort)changedDevices[0].Id, (ushort)changedDevices[changedDevices.Count - 1].Id);
            /*foreach (var device in changedDevices)
            {
                
            }*/
        }

        private async void InitializeDevice(BacNetDevice device, bool reInitialize = false)
        {
            if (!reInitialize)
                if (device.Status == DeviceStatus.Ready || device.Status == DeviceStatus.Initializing || device.Status == DeviceStatus.NotFound) return;
            if (reInitialize)
                //_reInitializeTimer.Stop();
            device.Status = DeviceStatus.Initializing;
            if (device.Address == null)
            {
                _network.WhoIs((ushort)device.Id, (ushort)device.Id);
                _waitForAddress.WaitOne(3000);
            }
            if (device.Address != null)
                device.ReadSupportedServices();
            if (device.Status == DeviceStatus.Ready)
            {
                OnFoundDeviceEvent(device.Id);
            }
            else
            {
                device.Status = DeviceStatus.NotFound;
                //_reInitializeTimer.Start();
            }
        }
    }
}
