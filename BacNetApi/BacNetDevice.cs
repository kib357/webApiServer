using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.AppService;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi.AccessControl;
using BacNetApi.Data;
using BacNetApi.Schedules;

namespace BacNetApi
{
    public class BacNetDevice
    {
        private readonly BacNet _network;
        private volatile DeviceStatus _status;
        private volatile SubscriptionStatus _subscriptionStatus;
        private readonly ObservableCollection<PrimitiveProperty> _subscriptionList;
        public readonly object SyncRoot = new Object();
        private volatile bool _trackState;

        public BACnetRemoteAddress Address { get; set; }
        public uint Id { get; private set; }
        public string Title { get; private set; }
        public SubscriptionStatus SubscriptionState { get { return _subscriptionStatus; } }
        public PrimitiveObjectIndexer Objects { get; private set; }
        public UserIndexer Users { get; private set; }
        public AccessGroupIndexer AccessGroups { get; private set; }
        public ScheduleIndexer Schedules { get; private set; }

        private List<string> _objectList = new List<string>(); 
        public List<string> ObjectList
        {
            get { return _objectList; }
            private set { _objectList = value; }
        }

        public void UpdateObjectList()
        {
            var responce = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ObjectList);
            var res = new List<string>();
            if (responce != null)
                    foreach (BACnetObjectId objectId in responce.Where(s => s is BACnetObjectId))
                        res.Add(objectId.ToString2());
            ObjectList = res;
        }

        public BacnetSegmentation Segmentation { get; set; }
        public List<BacnetServicesSupported> ServicesSupported { get; set; }
        public ApduSettings ApduSetting { get; set; }

        public DeviceStatus Status
        {
            get { return _status; }
            internal set
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
            Schedules = new ScheduleIndexer(this);
            Status = DeviceStatus.NotInitialized;
            _subscriptionStatus = SubscriptionStatus.Stopped;
            _subscriptionList = new ObservableCollection<PrimitiveProperty>();
            _subscriptionList.CollectionChanged += OnSubscriptionListChanged;
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

        public void SetAddress(BACnetRemoteAddress source, BACnetEnumerated segmentationSupported, ApduSettings settings)
        {
            Address = source;
            Segmentation = (BacnetSegmentation)segmentationSupported.Value;
            ApduSetting = settings;
        }

        internal void ReadSupportedServices()
        {
            if (Address == null) throw new Exception("Attemping to read services list before getting device address");
            var data = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported);
            if (data == null || data.Count != 1) return;
            var services = data[0];
            if (!(services is BACnetBitString)) return;
            ServicesSupported = new List<BacnetServicesSupported>();
            var value = (services as BACnetBitString).Value;
            for (int i = 0; i < value.Count && i < (int)BacnetServicesSupported.MaxBacnetServicesSupported; i++)
            {
                if (value[i])
                    ServicesSupported.Add((BacnetServicesSupported)i);
            }
            Status = DeviceStatus.Standby;
        }

        private void Initialize()
        {
            if (_status == DeviceStatus.Standby || _status == DeviceStatus.Online) return;
            _network.Manager.SearchDevice(Id);
        }

        internal void StartTracking()
        {
            if (_trackState == false)
            {
                _trackState = true;
                Task.Factory.StartNew(TrackDeviceState, TaskCreationOptions.LongRunning);
            }
        }

        private int _trackCount = 0;
        private int _faultCount = 0;
        private void TrackDeviceState()
        {
            while (_trackState)
            {
                _trackCount++;
                var data = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ObjectName);
                if (data != null && data.Count == 1 && data[0] is BACnetCharacterString)
                {
                    _faultCount = 0;
                    var name = data[0] as BACnetCharacterString;
                    var newTitle = name.Value;
                    LastUpdated = DateTime.Now;
                    if (Title != newTitle || _status != DeviceStatus.Online)
                    {
                        Title = newTitle;
                        Status = DeviceStatus.Online;
                    }
                    _network.OnNetworkModelChangedEvent();
                }
                else
                {
                    _faultCount++;
                    if (_faultCount == 6 && _status != DeviceStatus.Fault)
                    {
                        Status = DeviceStatus.Fault;
                        LastUpdated = DateTime.Now;
                        _network.OnNetworkModelChangedEvent();
                    }
                }
                if (_trackCount % 12 == 0 || _trackCount == 1)
                    UpdateObjectList();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        #endregion

        #region Subscription

        private void OnSubscriptionListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_subscriptionList.Count > 0 && _subscriptionStatus == SubscriptionStatus.Stopped)
            {
                _subscriptionStatus = SubscriptionStatus.Initializing;
            }
            if (_subscriptionList.Count == 0)
            {
                _subscriptionStatus = SubscriptionStatus.Stopped;
            }
        }

        internal void StartSubscription()
        {            
            if (ServicesSupported.Contains(BacnetServicesSupported.SubscribeCOVProperty))
            {
                _subscriptionStatus = SubscriptionStatus.CovProperty;
                Task.Factory.StartNew(COVPropertySubscription, TaskCreationOptions.LongRunning);
                return;
            }
            if (ServicesSupported.Contains(BacnetServicesSupported.SubscribeCOV))
            {
                _subscriptionStatus = SubscriptionStatus.Cov;
                if (ServicesSupported.Contains(BacnetServicesSupported.ReadPropMultiple))
                {
                    _subscriptionStatus = SubscriptionStatus.CovAndRpm;
                    Task.Factory.StartNew(RPMPolling, TaskCreationOptions.LongRunning);
                }
                else if (ServicesSupported.Contains(BacnetServicesSupported.ReadProperty))
                {
                    _subscriptionStatus = SubscriptionStatus.CovAndRp;
                    Task.Factory.StartNew(ReadPropertyPolling, TaskCreationOptions.LongRunning);
                }
                Task.Factory.StartNew(COVSubscription, TaskCreationOptions.LongRunning);
                return;
            }
            if (ServicesSupported.Contains(BacnetServicesSupported.ReadPropMultiple))
            {
                _subscriptionStatus = SubscriptionStatus.Rpm;
                Task.Factory.StartNew(RPMPolling, TaskCreationOptions.LongRunning);
                return;
            }
            if (ServicesSupported.Contains(BacnetServicesSupported.ReadProperty))
            {
                _subscriptionStatus = SubscriptionStatus.Rp;
                Task.Factory.StartNew(ReadPropertyPolling, TaskCreationOptions.LongRunning);
                return;
            }
            _subscriptionStatus = SubscriptionStatus.NoServicesSupported;
        }

        private void ReadPropertyPolling()
        {
            var pollingObjects = new List<PrimitiveProperty>();
            while (_subscriptionStatus != SubscriptionStatus.Stopped && _subscriptionStatus != SubscriptionStatus.Initializing)
            {
                lock (SyncRoot)
                    pollingObjects = ServicesSupported.Contains(BacnetServicesSupported.SubscribeCOV) ?
                        _subscriptionList.Where(p => p.Id != (int)BacnetPropertyId.PresentValue).ToList() : _subscriptionList.ToList();
                foreach (var primitiveProperty in pollingObjects)
                    _network.BeginReadProperty(Address, primitiveProperty._primitiveObject, primitiveProperty.Id);
                Thread.Sleep(TimeSpan.FromSeconds(_network.Config.ReadPropertyPollingInterval));
            }
        }

        private void RPMPolling()
        {
            var pollingObjects = new List<PrimitiveProperty>();
            while (_subscriptionStatus != SubscriptionStatus.Stopped && _subscriptionStatus != SubscriptionStatus.Initializing)
            {
                lock (SyncRoot)
                    pollingObjects = ServicesSupported.Contains(BacnetServicesSupported.SubscribeCOV) ?
                        _subscriptionList.Where(p => p.Id != (int)BacnetPropertyId.PresentValue).ToList() : _subscriptionList.ToList();
                var objList = new Dictionary<PrimitiveObject, List<PrimitiveProperty>>();
                foreach (var primitiveProperty in pollingObjects)
                {
                    if (!objList.ContainsKey(primitiveProperty._primitiveObject))
                        objList.Add(primitiveProperty._primitiveObject, new List<PrimitiveProperty>());
                    objList[primitiveProperty._primitiveObject].Add(primitiveProperty);
                }
                _network.BeginReadPropertyMultiple(Address, objList, ApduSetting);
                Thread.Sleep(TimeSpan.FromSeconds(_network.Config.RPMPollingInterval));
            }
        }

        private void COVSubscription()
        {
            var covObjects = new List<PrimitiveProperty>();
            while (_subscriptionStatus != SubscriptionStatus.Stopped && _subscriptionStatus != SubscriptionStatus.Initializing)
            {
                lock (SyncRoot)
                    covObjects = _subscriptionList.Where(p => p.Id == (int)BacnetPropertyId.PresentValue).ToList();
                foreach (var primitiveProperty in covObjects)
                    _network.SubscribeCOV(Address, primitiveProperty._primitiveObject.Id);
                Thread.Sleep(TimeSpan.FromSeconds(_network.Config.COVSubscriptionInterval));
            }
        }

        private void COVPropertySubscription()
        {
            var covObjects = new List<PrimitiveProperty>();
            while (_subscriptionStatus != SubscriptionStatus.Stopped && _subscriptionStatus != SubscriptionStatus.Initializing)
            {
                lock (SyncRoot)
                    covObjects = _subscriptionList.ToList();
                foreach (var primitiveProperty in covObjects)
                    _network.SubscribeCOVProperty(Address, primitiveProperty);
                Thread.Sleep(TimeSpan.FromSeconds(_network.Config.COVSubscriptionInterval));
            }
        }

        public void AddSubscriptionObject(PrimitiveProperty primitiveProperty)
        {
            lock (SyncRoot)
            {
                if (!_subscriptionList.Contains(primitiveProperty))
                    _subscriptionList.Add(primitiveProperty);
            }
        }

        public void RemoveSubscriptionObject(PrimitiveProperty primitiveProperty)
        {
            lock (SyncRoot)
            {
                if (_subscriptionList.Contains(primitiveProperty))
                    _subscriptionList.Remove(primitiveProperty);
            }
        }

        #endregion

        #region Services

        private bool CanNotSendRequest
        {
            get { return _status == DeviceStatus.Fault || _status == DeviceStatus.NotInitialized; }
        }

        public bool CreateObject(BacNetObject bacNetObject, List<BACnetPropertyValue> data)
        {
            Initialize();
            if (CanNotSendRequest) return false;
            return _network.CreateObject(Address, bacNetObject.Id, data, ApduSetting) != null;
        }

        public bool DeleteObject(BacNetObject bacNetObject)
        {
            Initialize();
            if (CanNotSendRequest) return false;
            return _network.DeleteObject(Address, bacNetObject.Id) != null;
        }

        public List<BACnetDataType> ReadProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, int arrayIndex = -1)
        {
            Initialize();
            if (CanNotSendRequest) return null;
            return _network.ReadProperty(Address, bacNetObject.Id, propertyId, arrayIndex);
        }

        public bool WriteProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, object value)
        {
            Initialize();
            if (CanNotSendRequest) return false;
            return _network.WriteProperty(Address, bacNetObject, propertyId, value, ApduSetting);
        }

        public void BeginWriteProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId, object value)
        {
            Initialize();
            if (CanNotSendRequest) return;
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
            if (CanNotSendRequest) return false;
            return _network.WritePropertyMultiple(Address, objectIdWithValues, ApduSetting);
        }

        #endregion
    }
}