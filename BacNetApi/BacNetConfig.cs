using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BACsharp.DataLink;
using Nini.Config;

namespace BacNetApi
{
    public class BacNetConfig
    {
        private XmlConfigSource _configSource;

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
            try
            {
                _configSource = new XmlConfigSource("BACnet.xml") { AutoSave = true };
            }
            catch (Exception ex)
            {
                WriteDefaultConfig();
            }       
            Initialize();
        }

        private void WriteDefaultConfig()
        {
            _configSource = new XmlConfigSource();

            string address = string.Empty;
            var port = DataLinkPort.GetAllDataLinkPorts().FirstOrDefault(p => p.BACnetConnection == DataLinkType.BacnetIP);
            if (port != null)
                address = port.DataLinkAddress.ToString().Remove(port.DataLinkAddress.ToString().IndexOf(':'));

            _configSource.AddConfig("BACnet");
            _configSource.Configs["BACnet"].Set("IpAddress", address);
            _configSource.Configs["BACnet"].Set("RequestTimeOut", 5);

            _configSource.AddConfig("Tracking");
            _configSource.Configs["Tracking"].Set("TrackUnsubscribedDevices", false);
            _configSource.Configs["Tracking"].Set("QueryFaultCount", 6);
            _configSource.Configs["Tracking"].Set("QueryInterval", 5);

            _configSource.AddConfig("Searching");
            _configSource.Configs["Searching"].Set("ManageDeviceServicesInterval", 5);
            _configSource.Configs["Searching"].Set("SendWhoIsInterval", 3);
            _configSource.Configs["Searching"].Set("LostDevicesSearchInterval", 60);

            try
            {
                _configSource.Save("BACnet.xml");
            }
            catch {}           
        }

        private void Initialize()
        {
            IpAddress = _configSource.Configs["BACnet"].Get("IpAddress");
            RequestTimeOut = _configSource.Configs["BACnet"].GetInt("RequestTimeOut");
            TrackUnsubscribedDevices = _configSource.Configs["Tracking"].GetBoolean("TrackUnsubscribedDevices");
            TrackingQueryFaultCount = _configSource.Configs["Tracking"].GetInt("QueryFaultCount");
            TrackingQueryInterval = _configSource.Configs["Tracking"].GetInt("QueryInterval");
            ManageDeviceServicesInterval = _configSource.Configs["Searching"].GetInt("ManageDeviceServicesInterval");
            SendWhoIsInterval = _configSource.Configs["Searching"].GetInt("SendWhoIsInterval");
            LostDevicesSearchInterval = _configSource.Configs["Searching"].GetInt("LostDevicesSearchInterval");
        }
    }
}
