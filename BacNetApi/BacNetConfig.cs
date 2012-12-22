using System;
using System.Linq;
using BACsharp.DataLink;
using Nini.Config;

namespace BacNetApi
{
    public class BacNetConfig
    {
        private XmlConfigSource _configSource;

        private const bool DefaultTrackUnsubscribedDevices = false;
        private const int DefaultRequestTimeOut = 5;
        private const int DefaultTrackingQueryFaultCount = 6;
        private const int DefaultTrackingQueryInterval = 5;
        private const int DefaultManageDeviceServicesInterval = 5;
        private const int DefaultSendWhoIsInterval = 3;
        private const int DefaultLostDevicesSearchInterval = 60;

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

            _configSource.AddConfig("BACnet");
            _configSource.Configs["BACnet"].Set("IpAddress", GetDefaultIpAddress());
            _configSource.Configs["BACnet"].Set("RequestTimeOut", DefaultRequestTimeOut);

            _configSource.AddConfig("Tracking");
            _configSource.Configs["Tracking"].Set("TrackUnsubscribedDevices", DefaultTrackUnsubscribedDevices);
            _configSource.Configs["Tracking"].Set("QueryFaultCount", DefaultTrackingQueryFaultCount);
            _configSource.Configs["Tracking"].Set("QueryInterval", DefaultTrackingQueryInterval);

            _configSource.AddConfig("Searching");
            _configSource.Configs["Searching"].Set("ManageDeviceServicesInterval", DefaultManageDeviceServicesInterval);
            _configSource.Configs["Searching"].Set("SendWhoIsInterval", DefaultSendWhoIsInterval);
            _configSource.Configs["Searching"].Set("LostDevicesSearchInterval", DefaultLostDevicesSearchInterval);

            try
            {
                _configSource.Save("BACnet.xml");
            }
            catch { }
        }

        private static string GetDefaultIpAddress()
        {
            string address = string.Empty;
            var port = DataLinkPort.GetAllDataLinkPorts().FirstOrDefault(p => p.BACnetConnection == DataLinkType.BacnetIP);
            if (port != null)
                address = port.DataLinkAddress.ToString().Remove(port.DataLinkAddress.ToString().IndexOf(':'));
            return address;
        }

        private void Initialize()
        {
            if (!_configSource.Configs["BACnet"].Contains("IpAddress"))
                _configSource.Configs["BACnet"].Set("IpAddress", GetDefaultIpAddress());
            IpAddress = _configSource.Configs["BACnet"].Get("IpAddress");

            if (!_configSource.Configs["BACnet"].Contains("RequestTimeOut"))
                _configSource.Configs["BACnet"].Set("RequestTimeOut", DefaultRequestTimeOut);
            RequestTimeOut = _configSource.Configs["BACnet"].GetInt("RequestTimeOut");

            if (!_configSource.Configs["Tracking"].Contains("TrackUnsubscribedDevices"))
                _configSource.Configs["Tracking"].Set("TrackUnsubscribedDevices", DefaultTrackUnsubscribedDevices);
            TrackUnsubscribedDevices = _configSource.Configs["Tracking"].GetBoolean("TrackUnsubscribedDevices");

            if (!_configSource.Configs["Tracking"].Contains("QueryFaultCount"))
                _configSource.Configs["Tracking"].Set("QueryFaultCount", DefaultTrackingQueryFaultCount);
            TrackingQueryFaultCount = _configSource.Configs["Tracking"].GetInt("QueryFaultCount");

            if (!_configSource.Configs["Tracking"].Contains("QueryInterval"))
                _configSource.Configs["Tracking"].Set("QueryInterval", DefaultTrackingQueryInterval);
            TrackingQueryInterval = _configSource.Configs["Tracking"].GetInt("QueryInterval");

            if (!_configSource.Configs["Searching"].Contains("ManageDeviceServicesInterval"))
                _configSource.Configs["Searching"].Set("ManageDeviceServicesInterval", DefaultManageDeviceServicesInterval);
            ManageDeviceServicesInterval = _configSource.Configs["Searching"].GetInt("ManageDeviceServicesInterval");

            if (!_configSource.Configs["Searching"].Contains("SendWhoIsInterval"))
                _configSource.Configs["Searching"].Set("SendWhoIsInterval", DefaultSendWhoIsInterval);
            SendWhoIsInterval = _configSource.Configs["Searching"].GetInt("SendWhoIsInterval");

            if (!_configSource.Configs["Searching"].Contains("LostDevicesSearchInterval"))
                _configSource.Configs["Searching"].Set("LostDevicesSearchInterval", DefaultLostDevicesSearchInterval);
            LostDevicesSearchInterval = _configSource.Configs["Searching"].GetInt("LostDevicesSearchInterval");
        }
    }
}
