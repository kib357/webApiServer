using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types.Primitive;

namespace BacNetApi
{
    public class BacNetDevice 
    {        
        private readonly BacNet                             _network;
        private volatile DeviceStatus                       _status;
        private volatile SubscriptionStatus                 _subscriptionStatus;
        private readonly ObservableCollection<BacNetObject> _subscriptionList;
        private readonly AutoResetEvent _waitForAddress = new AutoResetEvent(false);

        public BACnetAddress                 Address { get; set; }
        public uint                          Id { get; private set; }        
        public BacNetObjectIndexer           Objects { get; private set; }
        public BacnetSegmentation            Segmentation { get; set; }
        public List<BacnetServicesSupported> ServicesSupported { get; set; } 

        public BacNetDevice(uint id, BacNet network)
        {
            Id = id;
            _network = network;
            Objects = new BacNetObjectIndexer(this);
            ServicesSupported = new List<BacnetServicesSupported>();
            _status = DeviceStatus.NotInitialized;
            _subscriptionStatus = SubscriptionStatus.Stopped;
            _subscriptionList = new ObservableCollection<BacNetObject>();
            _subscriptionList.CollectionChanged += OnSubscriptionListChanged;
        }

        private void OnSubscriptionListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_subscriptionList.Count > 0 && _subscriptionStatus == SubscriptionStatus.Stopped)
            {
                _subscriptionStatus = SubscriptionStatus.Initializing;
                Task.Run(() => StartSubscription());
            }
            if (_subscriptionList.Count == 0)
            {
                StopSubsciption();
            }
        }        

        private async void StartSubscription()
        {
            await WaitForInitialization();
            if (ServicesSupported.Contains(BacnetServicesSupported.SubscribeCOV))
                Task.Run(() => COVSubscription());
            else
            //    if (ServicesSupported.Contains(BacnetServicesSupported.ReadPropMultiple))
            //        RPMPolling();
            //    else
                    Task.Run(() => ReadPropertyPolling());
            _subscriptionStatus = SubscriptionStatus.Running;
        }

        private void StopSubsciption()
        {
            _subscriptionStatus = SubscriptionStatus.Stopped;
        }

        private void ReadPropertyPolling()
        {
            while (_subscriptionStatus == SubscriptionStatus.Running)
            {
                foreach (var bacNetObject in _subscriptionList)
                {
                    _network.BeginReadProperty(Address, bacNetObject, BacnetPropertyId.PresentValue);
                }
                Thread.Sleep(5000);
            }
        }

        private void RPMPolling()
        {
            throw new NotImplementedException();
            while (_subscriptionStatus == SubscriptionStatus.Running)
            {
                //_network.ReadPropertyMutiple(Address, _subscriptionList);
                Thread.Sleep(5000);
            }
        }

        private void COVSubscription()
        {
            while (_subscriptionStatus == SubscriptionStatus.Running)
            {
                foreach (var bacNetObject in _subscriptionList)
                {
                    _network.SubscribeCOV(Address, bacNetObject);
                }
                Thread.Sleep(TimeSpan.FromSeconds(1800));
            }
        }

        private void Initialize()
        {
            if (_status == DeviceStatus.Ready || _status == DeviceStatus.Initializing) return;
            _status = DeviceStatus.Initializing;
            if (Address != null || SearchDevice())
            {                
                var services = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported);
                if (services is BACnetBitString)
                {
                    var value = (services as BACnetBitString).Value;
                    for (int i = 0; i <value.Length && i < (int)BacnetServicesSupported.MaxBacnetServicesSupported; i++)
                    {
                        if (value[i])
                            ServicesSupported.Add((BacnetServicesSupported)i);
                    }
                    _status = DeviceStatus.Ready;
                    return;
                }
                //todo: implement reading of object list when segmentation support will enabled in provider
                //var objects = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ObjectList);
            }
            _status = DeviceStatus.NotInitialized;
        }

        private bool SearchDevice()
        {
            _network.WhoIs((ushort)Id,(ushort)Id);
            _waitForAddress.WaitOne(2000);
            return Address != null;
        }

        private async Task WaitForInitialization()
        {
            await Task.Run(() =>
                               {
                                   var minutes = 1;
                                   while (_status != DeviceStatus.Ready)
                                   {
                                       Initialize();
                                       if (_status == DeviceStatus.Ready) break;
                                       Thread.Sleep(TimeSpan.FromMinutes(minutes));
                                       if (minutes < 127) minutes *= 2;
                                   }
                               });
        }

        public bool CreateObject(BacNetObject bacNetObject)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.CreateObject(Address, bacNetObject.Id) != null;
        }

        public bool DeleteObject(BacNetObject bacNetObject)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.DeleteObject(Address, bacNetObject.Id) != null;
        }

        public object ReadProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, int arrayIndex = -1)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return null;
            return _network.ReadProperty(Address, bacNetObject.Id, propertyId, arrayIndex);
        }

        public void SetAddress(BACnetAddress source, BACnetEnumerated segmentationSupported)
        {
            Address = source;
            Segmentation = (BacnetSegmentation)segmentationSupported.Value;
            _waitForAddress.Set();
        }

        public void AddSubscriptionObject(BacNetObject bacNetObject)
        {
            if (!_subscriptionList.Contains(bacNetObject))
            _subscriptionList.Add(bacNetObject);
        }

        public void RemoveSubscriptionObject(BacNetObject bacNetObject)
        {
            if (_subscriptionList.Contains(bacNetObject))
                _subscriptionList.Add(bacNetObject);
        }

        public bool WriteProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, object value)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.WriteProperty(Address, bacNetObject, propertyId, value);
        }
    }
}