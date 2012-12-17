using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.AppService;
using BACsharp.Types.Primitive;

namespace BacNetApi
{
    internal class DeviceFinder
    {
        private readonly BacNet _network;
        public readonly Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>> _finded = new Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>>();
        private readonly ObservableCollection<uint> _search = new ObservableCollection<uint>();
        private readonly List<uint> _lost = new List<uint>();
        private readonly object SyncRoot = new object();
        private volatile bool Searching = false;


        public DeviceFinder(BacNet network)
        {
            _network = network;
            _search.CollectionChanged += SearchListChanged;
            _network.WhoIs();
            Task.Factory.StartNew(ReadServices, TaskCreationOptions.LongRunning);
        }

        private void SearchListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var s = sender as ObservableCollection<uint>;
            if (s != null && s.Count > 0 && !Searching)
            {
                Searching = true;
                Task.Factory.StartNew(Search, TaskCreationOptions.LongRunning);                
            }
            else
            {
                Searching = false;
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

        private void ReadServices()
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

                foreach (var d in iterationDevices)
                {
                    //var data = _network.ReadProperty(d.Value.Item1, d.Key + ".DEV" + d.Key, BacnetPropertyId.ProtocolServicesSupported);
                    //if (data == null || data.Count != 1) continue;
                    //var services = data[0];
                    //if (!(services is BACnetBitString)) continue;
                    //_network[d.Key].ServicesSupported = new List<BacnetServicesSupported>();
                    //var value = (services as BACnetBitString).Value;
                    //for (int i = 0; i < value.Length && i < (int)BacnetServicesSupported.MaxBacnetServicesSupported; i++)
                    //{
                    //    if (value[i])
                    //        _network[d.Key].ServicesSupported.Add((BacnetServicesSupported)i);                            
                    //}
                    //_network[d.Key].Status = DeviceStatus.Standby;
                    if (_network[d.Key].Status == DeviceStatus.NotInitialized)
                        _network[d.Key].ReadSupportedServices();
                }

                foreach (var d in iterationDevices)
                {
                    if (_network[d.Key].Status == DeviceStatus.Standby)
                        _network[d.Key].StartTracking();
                }                
            }
        }

        private void Search()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                if (!Searching) return;
                List<uint> iterationDevices;
                lock (SyncRoot)
                {
                    iterationDevices = new List<uint>(_search);
                }

                foreach (var iterationDevice in iterationDevices)
                {
                    _network.WhoIs((ushort) iterationDevice, (ushort) iterationDevice);
                }                
            }
        }
    }
}
