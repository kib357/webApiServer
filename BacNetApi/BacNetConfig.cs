using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nini.Config;

namespace BacNetApi
{
    public class BacNetConfig
    {
        private readonly XmlConfigSource _configSource;

        public string IpAddress { get; set; }
        public bool TrackUnsubscribedDevices { get; set; }
        public int RequestTimeOut { get; set; }
        public int TrackingQueryFaultCount { get; set; }
        public int TrackingQueryInterval { get; set; }
        public int ManageDeviceServicesInterval { get; set; }
        public int SendWhoIsInterval { get; set; }
        public int LostDevicesSearchInterval { get; set; }

        public BacNetConfig()
        {
            _configSource = new XmlConfigSource("BACnet.xml") {AutoSave = true};
        }

        private void InitDefault()
        {
            TrackUnsubscribedDevices = false;
            RequestTimeOut = 5;
            TrackingQueryFaultCount = 6;
            TrackingQueryInterval = 5;
            ManageDeviceServicesInterval = 5;
            SendWhoIsInterval = 3;
            LostDevicesSearchInterval = 60;
        }
    }
}
