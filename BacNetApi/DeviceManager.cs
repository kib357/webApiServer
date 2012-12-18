using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.AppService;
using BACsharp.Types.Primitive;

namespace BacNetApi
{
    internal class DeviceManager
    {
        private readonly BacNet _network;
        public readonly Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>> _finded = new Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>>();
        private readonly ObservableCollection<uint> _search = new ObservableCollection<uint>();
        private readonly object SyncRoot = new object();


        public DeviceManager(BacNet network)
        {
            _network = network;
            Task.Factory.StartNew(Search, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(StartDeviceServices, TaskCreationOptions.LongRunning);
        }

        public void SearchDevice(uint instance)
        {
            lock (SyncRoot)
            {
                if (_finded.ContainsKey(instance))
                    _network[instance].SetAddress(_finded[instance].Item1, _finded[instance].Item2,
                                                  _finded[instance].Item3);
                else
                {
                    if (!_search.Contains(instance))
                        _search.Add(instance);
                }
            }
        }

        public void DeviceLocated(uint instance, BACnetAddress source, BACnetEnumerated segmentationSupported, ApduSettings settings)
        {
            lock (SyncRoot)
            {
                if (_search.Contains(instance))
                    _search.Remove(instance);
                if (!_finded.ContainsKey(instance))
                    _finded.Add(instance, new Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>(source, segmentationSupported, settings));
                else
                    _finded[instance] = new Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>(source, segmentationSupported, settings);
            }
            _network[instance].SetAddress(source, segmentationSupported, settings);
        }

        private void StartDeviceServices()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>> iterationDevices;
                lock (SyncRoot)
                {
                    iterationDevices =
                        new Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>>(_finded);
                }

                //Это - очень важные форичи. Не дай боже какой-нибудь падла решит эти форичи в один объединить, 
                //очень плохо это для него закончится
                foreach (var d in iterationDevices)
                {
                    if (_network[d.Key].Status == DeviceStatus.NotInitialized)
                        _network[d.Key].ReadSupportedServices();
                }
                foreach (var d in iterationDevices)
                {
                    if (_network[d.Key].Status == DeviceStatus.Standby)
                        _network[d.Key].StartTracking();
                }
                foreach (var d in iterationDevices)
                {
                    if (_network[d.Key].Status == DeviceStatus.Online &&
                        _network[d.Key].SubscriptionState == SubscriptionStatus.Initializing)
                        _network[d.Key].StartSubscription();
                }
            }
        }

        private void Search()
        {
            _network.WhoIs();
            Thread.Sleep(TimeSpan.FromSeconds(5));
            while (true)
            {
                List<uint> iterationDevices;
                lock (SyncRoot)
                {
                    iterationDevices =
                        new Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>>(_finded).Keys.OrderBy(k => k).ToList();
                }

                uint min = 0;
                foreach (uint iDev in iterationDevices)
                {
                    if (min < iDev - 1)
                    {
                        _network.WhoIs((min + 1), (iDev - 1));
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    min = iDev;
                }
                if (min < 4194303)
                    _network.WhoIs(min + 1, 4194303);
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }
        }
    }
}
