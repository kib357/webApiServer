using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using BACsharp;
using BACsharp.AppService;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi.AccessControl;
using BacNetApi.Data;

namespace BacNetApi
{
    public class BacNetDevice 
    {        
        private readonly BacNet                             _network;
        private volatile DeviceStatus                       _status;
        private volatile SubscriptionStatus                 _subscriptionStatus;
        private readonly ObservableCollection<BacNetObject> _subscriptionList;
        private readonly AutoResetEvent                     _waitForAddress = new AutoResetEvent(false);
        public readonly object                              SyncRoot = new Object();
        private volatile bool                               _trackState;
        private readonly DispatcherTimer                    _reInitializeTimer;

        public BACnetAddress                 Address { get; set; }
        public uint                          Id { get; private set; }
        public string                        Title { get; private set; }        
        public SubscriptionStatus            SubscriptionState { get { return _subscriptionStatus; } }
        public PrimitiveObjectIndexer           Objects { get; private set; }
        public UserIndexer                   Users { get; private set; }
        public AccessGroupIndexer            AccessGroups { get; private set; }
        public List<string>                  ObjectList { get; private set; }
        public BacnetSegmentation            Segmentation { get; set; }
        public List<BacnetServicesSupported> ServicesSupported { get; set; }
        public ApduSettings                  ApduSetting { get; set; }

        public DeviceStatus Status
        {
            get { return _status; }
            private set
            {
                _network.OnNetworkModelChangedEvent();
                _status = value;
            }
        }

        public BacNetDevice(uint id, BacNet network)
        {
            Id = id;
            Title = string.Empty;
            _network = network;
            Objects = new PrimitiveObjectIndexer(this);
            Users = new UserIndexer(this);
            AccessGroups = new AccessGroupIndexer(this);
            ObjectList = new List<string>();
            Status = DeviceStatus.NotInitialized;
            _subscriptionStatus = SubscriptionStatus.Stopped;
            _subscriptionList = new ObservableCollection<BacNetObject>();
            _subscriptionList.CollectionChanged += OnSubscriptionListChanged;

            _reInitializeTimer = new DispatcherTimer { Interval = new TimeSpan(0, 1, 0, 0) };
            _reInitializeTimer.Tick += ReInitializeDevice;
            _reInitializeTimer.Start();
        }

        private void ReInitializeDevice(object sender, EventArgs e)
        {
            if (Status == DeviceStatus.NotFound)
                Initialize(true);
        }

        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            internal set
            {
                if (_lastUpdated != DateTime.MinValue && value - _lastUpdated < TimeSpan.FromSeconds(5)) return;
                _lastUpdated = value;
                _network.OnNetworkModelChangedEvent();
            }
        }

        #region Initialization

        public void SetAddress(BACnetAddress source, BACnetEnumerated segmentationSupported, ApduSettings settings)
        {
            Address = source;
            Segmentation = (BacnetSegmentation)segmentationSupported.Value;
            ApduSetting = settings;
            if (Status != DeviceStatus.Online)
                Status = DeviceStatus.Ready;
            _waitForAddress.Set();            
        }

        private void ReadSupportedServices()
        {
            if (Address == null) throw new Exception("Attemping to read services list before getting device address");
            var data = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported);
            if (data.Count != 1) return;
            var services = data[0];
            if (services is BACnetBitString)
            {
                ServicesSupported = new List<BacnetServicesSupported>();
                var value = (services as BACnetBitString).Value;
                for (int i = 0; i < value.Length && i < (int)BacnetServicesSupported.MaxBacnetServicesSupported; i++)
                {
                    if (value[i])
                        ServicesSupported.Add((BacnetServicesSupported)i);
                }                
                Status = DeviceStatus.Online;                
            }
        }

        private void Initialize(bool reInitialize = false)
        {
            if (!reInitialize)
                if (_status == DeviceStatus.Ready || _status == DeviceStatus.Initializing || _status == DeviceStatus.NotFound || _status == DeviceStatus.Online) return;
            if(reInitialize)
                _reInitializeTimer.Stop();
            Status = DeviceStatus.Initializing;
            if (Address == null)
            {
                _network.Finder.SearchDevice(Id);
                _waitForAddress.WaitOne(3000);
            }
            if (Address != null)
                ReadSupportedServices();
            if (_status == DeviceStatus.Ready)
            {
                _trackState = true;
                Task.Factory.StartNew(TrackDeviceState, TaskCreationOptions.LongRunning);
            }
            else
            {
                Status = DeviceStatus.NotFound;
                _reInitializeTimer.Start();
            }
        }

        private async Task WaitForInitialization()
        {
            await Task.Factory.StartNew(() =>
            {
                var minutes = 1;
                while(true)
                {
                    Initialize();
                    if (_status == DeviceStatus.Ready) break;
                    Thread.Sleep(TimeSpan.FromMinutes(minutes));
                    if (minutes < 127) minutes *= 2;
                }
            }, TaskCreationOptions.LongRunning);
        }

        private int _trackCount = 0;
        private void TrackDeviceState()
        {
            while (_trackState)
            {
                _trackCount++;
                var data = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ObjectName);
                if (data != null && data.Count == 1 && data[0] is BACnetCharacterString)
                {
                    var name = data[0] as BACnetCharacterString;
                    var newTitle = name.Value;
                    if (Title != newTitle || _status != DeviceStatus.Ready)
                    {
                        Title = newTitle;
                        Status = DeviceStatus.Online;
                        LastUpdated = DateTime.Now;
                        _network.OnNetworkModelChangedEvent();
                    }
                }
                else
                {
                    if (_status != DeviceStatus.Fault)
                    {
                        Status = DeviceStatus.Fault;
                        LastUpdated = DateTime.Now;
                        _network.OnNetworkModelChangedEvent();
                    }
                }
                if (_trackCount % 6 == 0 || _trackCount == 1)
                    GetObjectList();
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        private void GetObjectList()
        {
            ObjectList.Clear();
            var responce = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ObjectList);
            if (responce is List<BACnetDataType>)
            {
                var objectList = responce as List<BACnetDataType>;
                foreach (var baCnetDataType in objectList)
                {
                    var baCnetObjectId = baCnetDataType as BACnetObjectId;
                    if (baCnetObjectId != null)
                        ObjectList.Add(baCnetObjectId.ToString2());
                }
            }
        }

        #endregion

        #region Subscription

        private void OnSubscriptionListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_subscriptionList.Count > 0 && _subscriptionStatus == SubscriptionStatus.Stopped)
            {
                _subscriptionStatus = SubscriptionStatus.Initializing;
                Task.Factory.StartNew(StartSubscription, TaskCreationOptions.LongRunning);
            }
            if (_subscriptionList.Count == 0)
            {
                StopSubsciption();
            }
        }        

        private async void StartSubscription()
        {
            await WaitForInitialization();
            _subscriptionStatus = SubscriptionStatus.Running;
            if (ServicesSupported.Contains(BacnetServicesSupported.SubscribeCOV))
                COVSubscription();
            else
                if (ServicesSupported.Contains(BacnetServicesSupported.ReadPropMultiple))
                    RPMPolling();
                else
                ReadPropertyPolling();
        }

        private void StopSubsciption()
        {
            _subscriptionStatus = SubscriptionStatus.Stopped;
        }

        private void ReadPropertyPolling()
        {
            while (_subscriptionStatus == SubscriptionStatus.Running)
            {
                lock (SyncRoot)
                {
                    foreach (var bacNetObject in _subscriptionList)
                    {
                        _network.BeginReadProperty(Address, bacNetObject, BacnetPropertyId.PresentValue);
                    }
                }
                Thread.Sleep(10000);
            }
        }

        private void RPMPolling()
        {
            while (_subscriptionStatus == SubscriptionStatus.Running)
            {
                lock (SyncRoot)
                {
                    _network.BeginReadPropertyMultiple(Address, _subscriptionList.ToList(), ApduSetting);
                }
                Thread.Sleep(10000);
            }
        }

        private void COVSubscription()
        {
            while (_subscriptionStatus == SubscriptionStatus.Running)
            {
                lock (SyncRoot)
                {
                    foreach (var bacNetObject in _subscriptionList)
                    {
                        _network.SubscribeCOV(Address, bacNetObject);
                    }
                }
                Thread.Sleep(TimeSpan.FromSeconds(1800));
            }
        }

        public void AddSubscriptionObject(BacNetObject bacNetObject)
        {
            lock (SyncRoot)
            {
                if (!_subscriptionList.Contains(bacNetObject))
                    _subscriptionList.Add(bacNetObject);
            }
        }

        public void RemoveSubscriptionObject(BacNetObject bacNetObject)
        {
            lock (SyncRoot)
            {
                if (_subscriptionList.Contains(bacNetObject))
                    _subscriptionList.Add(bacNetObject);
            }
        }

        #endregion        

        #region Services

        public bool CreateObject(BacNetObject bacNetObject, List<BACnetPropertyValue> data)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.CreateObject(Address, bacNetObject.Id, data, ApduSetting) != null;
        }

        public bool DeleteObject(BacNetObject bacNetObject)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.DeleteObject(Address, bacNetObject.Id) != null;
        }

        public List<BACnetDataType> ReadProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, int arrayIndex = -1)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return null;
            return _network.ReadProperty(Address, bacNetObject.Id, propertyId, arrayIndex);
        }        

        public bool WriteProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, object value)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.WriteProperty(Address, bacNetObject, propertyId, value, ApduSetting);
        }

        public void BeginWriteProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, object value)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return;
            _network.BeginWriteProperty(Address, bacNetObject, propertyId, value, ApduSetting);
        }

        /// <summary>
        /// Метод для записи свойств нескольких объектов
        /// </summary>
        /// <param name="objectIdWithValues">
        /// Список объектов, каждый из которых содержит список свойств со значениями
        /// </param>
        /// <returns>
        /// true - если запись прошла успешно, иначе false
        /// </returns>
        public bool WritePropertyMultiple(Dictionary<string, Dictionary<BacnetPropertyId, object>> objectIdWithValues)
        {
            Initialize();
            if (_status != DeviceStatus.Ready) return false;
            return _network.WritePropertyMultiple(Address, objectIdWithValues, ApduSetting);
        }

        #endregion
    }
}