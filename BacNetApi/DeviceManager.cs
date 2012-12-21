using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public readonly Dictionary<uint, Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>> _finded = new Dictionary<uint, Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>>();
        private readonly ObservableCollection<uint> _search = new ObservableCollection<uint>();
        private readonly object SyncRoot = new object();
        private volatile bool _searchLostDevices = false;
        private volatile bool _searchingAllDevices = false;


        public DeviceManager(BacNet network)
        {
            _network = network;
            _search.CollectionChanged += SearchListChanged;            
            SearchAllDevices();
            Task.Factory.StartNew(StartDeviceServices, TaskCreationOptions.LongRunning);
        }

        internal void SearchAllDevices()
        {
            if (!_searchingAllDevices)
                Task.Factory.StartNew(Search, TaskCreationOptions.LongRunning);               
        }

        private void SearchListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var s = sender as ObservableCollection<uint>;
            if (s != null && s.Count > 0 && !_searchLostDevices)
            {
                _searchLostDevices = true;
                Task.Factory.StartNew(SearchLostDevices, TaskCreationOptions.LongRunning);
            }
            else
            {
                _searchLostDevices = false;
            }
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

        public void DeviceLocated(uint instance, BACnetRemoteAddress source, BACnetEnumerated segmentationSupported, ApduSettings settings)
        {
            lock (SyncRoot)
            {
                if (_search.Contains(instance))
                    _search.Remove(instance);
                if (!_finded.ContainsKey(instance))
                    _finded.Add(instance, new Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>(source, segmentationSupported, settings));
                else
                    _finded[instance] = new Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>(source, segmentationSupported, settings);
            }
            _network[instance].SetAddress(source, segmentationSupported, settings);
        }

        private void StartDeviceServices()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(_network.Config.ManageDeviceServicesInterval));
                Dictionary<uint, Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>> iterationDevices;
                lock (SyncRoot)
                {
                    iterationDevices =
                        new Dictionary<uint, Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>>(_finded);
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
                    if (_network[d.Key].Status == DeviceStatus.Standby &&
                        (_network[d.Key].SubscriptionState != SubscriptionStatus.Stopped || _network.Config.TrackUnsubscribedDevices))
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
            _searchingAllDevices = true;

            _network.WhoIs();
            Thread.Sleep(TimeSpan.FromSeconds(_network.Config.SendWhoIsInterval));

            List<uint> iterationDevices;
            lock (SyncRoot)
            {
                iterationDevices =
                    new Dictionary<uint, Tuple<BACnetRemoteAddress, BACnetEnumerated, ApduSettings>>(_finded).Keys.OrderBy(k => k).ToList();
            }

            uint min = 0;
            foreach (uint iDev in iterationDevices)
            {
                if (min < iDev - 1)
                {
                    _network.WhoIs((min + 1), (iDev - 1));
                    Thread.Sleep(TimeSpan.FromSeconds(_network.Config.SendWhoIsInterval));
                }
                min = iDev;
            }
            if (min < 4194303)
                _network.WhoIs(min + 1, 4194303);

            _searchingAllDevices = false;
        }

        private void SearchLostDevices()
        {
            while (_searchLostDevices)
            {
                Thread.Sleep(TimeSpan.FromSeconds(_network.Config.LostDevicesSearchInterval));
                List<uint> iterationDevices;
                lock (SyncRoot)
                {
                    iterationDevices = new List<uint>(_search);                        
                }

                foreach (uint iDev in iterationDevices)
                {
                    _network.WhoIs(iDev, iDev);
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }                
            }
        }
    }
}
