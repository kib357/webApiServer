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
        private readonly Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>> _finded = new Dictionary<uint, Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>>();
        private readonly ObservableCollection<uint> _search = new ObservableCollection<uint>();
        private readonly List<uint> _lost = new List<uint>();
        private readonly object SyncRoot = new object();
        private volatile bool Searching = false;


        public DeviceFinder(BacNet network)
        {
            _network = network;
            _search.CollectionChanged += SearchListChanged;
            _network.WhoIs();
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
                    _network[instance].SetAddress(_finded[instance].Item1, _finded[instance].Item2, _finded[instance].Item3);
                if (!_search.Contains(instance))
                    _search.Add(instance);
            }
        }

        public void DeviceLocated(uint instance, BACnetAddress source, BACnetEnumerated segmentationSupported, ApduSettings settings)
        {
            lock (SyncRoot)
            {
                if (_search.Contains(instance))
                    _search.Remove(instance);
                if (!_finded.ContainsKey(instance))
                    _finded.Add(instance, new Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>(source, segmentationSupported,settings));
                else
                    _finded[instance] = new Tuple<BACnetAddress, BACnetEnumerated, ApduSettings>(source, segmentationSupported, settings);
            }
            _network[instance].SetAddress(source, segmentationSupported, settings);
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
