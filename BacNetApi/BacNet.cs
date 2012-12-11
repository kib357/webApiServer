using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using BACsharp;
using BACsharp.AppService;
using BACsharp.AppService.ConfirmedServices;
using BACsharp.AppService.ErrorServices;
using BACsharp.AppService.UnconfirmedServices;
using BACsharp.DataLink;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi.Attributes;

namespace BacNetApi
{
    public enum SubscriptionStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2
    }

    public delegate void NotificationEventHandler(UnconfirmedEventNotificationRequest notification);
    public delegate void NetworkModelChangedEventHandler();
    public delegate void DeviceListChangedEventHandler(List<BacNetDevice> changedDevices);

    public class BacNet : IBacNetServices
    {
        private bool                         _initialized;
        private BaseAppServiceProvider       _bacNetProvider;
        private readonly List<BacNetDevice>  _deviceList = new List<BacNetDevice>();
        private readonly List<BacNetRequest> _requests = new List<BacNetRequest>();
        private DeviceFinder                 _deviceFinder;
        private BacnetDeviceComparer         _deviceComparer = new BacnetDeviceComparer();
        public List<BacNetDevice>            DeviceList { get { return _deviceList; } }
        public readonly object               SyncRoot = new Object();
        public event NotificationEventHandler NotificationEvent;
        
        public event NetworkModelChangedEventHandler NetworkModelChangedEvent;
        public event DeviceListChangedEventHandler DeviceListChangedEvent;

        internal void OnDeviceListChangedEvent(List<BacNetDevice> changedDevices)
        {
            DeviceListChangedEventHandler handler = DeviceListChangedEvent;
            if (handler != null) handler(changedDevices);
        }

        internal void OnNetworkModelChangedEvent()
        {
            NetworkModelChangedEventHandler handler = NetworkModelChangedEvent;
            if (handler != null) handler();
        }

        public BacNet(string address)
        {
            InitializeProvider(address);
        }

        private void InitializeProvider(string address)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(address, out ipAddress))
                StartProvider(ipAddress);
        }

        private void StartProvider(IPAddress address)
        {
            if (_initialized) return;
            _bacNetProvider = new BaseAppServiceProvider(new DataLinkPort(address));                        
            _bacNetProvider.OnIAmRequest += OnIamReceived;
            _bacNetProvider.OnReadPropertyAck += OnReadPropertyAckReceived;
            _bacNetProvider.OnReadPropertyMultipleAck += OnReadPropertyMultipleAckReceived;
            _bacNetProvider.OnError += OnErrorAckReceived;
            _bacNetProvider.OnSubscribeCOVAck += OnSubscribeCOVAck;
            _bacNetProvider.OnUnconfirmedCOVNotificationRequest += OnCOVNotification;
            _bacNetProvider.OnUnconfirmedEventNotificationRequest += OnEventNotification;
            _bacNetProvider.Start();
            _deviceFinder = new DeviceFinder(this);
            _initialized = true;
        }                

        public BacNetDevice this[uint i]
        {
            get
            {
                int index = _deviceList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new BacNetDevice(i, this);
                    _deviceList.Add(device);
                    _deviceList.Sort(_deviceComparer);
                    index = _deviceList.FindIndex(d => d.Id == i);
                }
                return _deviceList[index];
            }
            set
            {
                int index = _deviceList.FindIndex(d => d.Id == i);
                if (index < 0)
                    _deviceList.Add(value);
                else
                    _deviceList[index] = value;
                _deviceList.Sort(_deviceComparer);
            }
        }

        public List<BacNetDevice> OnlineSubscribedDevices
        {
            get { return _deviceList.Where(d => d.SubscriptionState == SubscriptionStatus.Running).ToList(); }
        }

        public List<BacNetDevice> SubscribedDevices
        {
            get { return _deviceList.Where(d => d.SubscriptionState != SubscriptionStatus.Stopped).ToList(); }
        }

        public void FindSeveral(List<uint> deviceList)
        {
            var devicesToFind = new List<BacNetDevice>();
            foreach (var deviceId in deviceList)
            {
                int index = _deviceList.FindIndex(d => d.Id == deviceId);
                if (index < 0)
                {
                    var device = new BacNetDevice(deviceId, this);
                    _deviceList.Add(device);
                    index = _deviceList.FindIndex(d => d.Id == deviceId);
                }
                devicesToFind.Add(_deviceList[index]);
            }
            _deviceList.Sort(_deviceComparer);
            OnDeviceListChangedEvent(devicesToFind);
        }

        #region Requests

        internal void WhoIs(ushort startAddress, ushort endAddress)
        {
            lock (SyncRoot)
            {
                //это такой специальный слип, который чтобы дельта не тупила когда мы её хуизами закидываем
                Thread.Sleep(30);
                _bacNetProvider.SendMessage(BACnetAddress.GlobalBroadcast(), new WhoIsRequest(startAddress, endAddress));
            }            
        }

        internal object ReadProperty(BACnetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId, int arrayIndex = -1)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(address), (int)bacnetPropertyId, arrayIndex);
            var readPropertyResponse = SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, readPropertyRequest) as ReadPropertyAck;
            if (readPropertyResponse == null) return null;
            var values = readPropertyResponse.PropertyValues;
            return values.Count == 1 ? (object) values[0] : values;
        }

        internal void BeginReadProperty(BACnetAddress bacAddress, BacNetObject bacObject, BacnetPropertyId bacnetPropertyId)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(bacObject.Id), (int)bacnetPropertyId);
            SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, readPropertyRequest, bacObject, false);
        }

        internal void BeginReadPropertyMultiple(BACnetAddress bacAddress, List<BacNetObject> objectList, ApduSettings settings)
        {
            var objList = new Dictionary<BACnetObjectId, List<BACnetPropertyReference>>();
            foreach (var bacObject in objectList)
            {
                objList.Add(BacNetObject.GetObjectIdByString(bacObject.Id), 
                    new List<BACnetPropertyReference>{new BACnetPropertyReference((int)BacnetPropertyId.PresentValue)});
            }
            var readPropertyMultipleRequest = new ReadPropertyMultipleRequest(objList);
            SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadPropMultiple, readPropertyMultipleRequest, objectList, false, settings);
        }

        internal bool WriteProperty(BACnetAddress bacAddress, BacNetObject bacNetObject, BacnetPropertyId bacnetPropertyId, object value, ApduSettings settings)
        {
            var objId = BacNetObject.GetObjectIdByString(bacNetObject.Id);
            List<BACnetDataType> valueByType = ConvertValueToBacnet(bacNetObject.Id, value, bacnetPropertyId);
            var writePropertyRequest = new WritePropertyRequest(objId, (int)bacnetPropertyId, valueByType, priority:10);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, writePropertyRequest, null, true, settings) == null;
        }

        internal bool WritePropertyMultiple(BACnetAddress bacAddress, Dictionary<string, Dictionary<BacnetPropertyId, object>> objectIdWithValues, ApduSettings settings)
        {
            var dataToWrite = new Dictionary<BACnetObjectId, List<BACnetPropertyValue>>();
            foreach (var obj in objectIdWithValues)
            {
                var objId = BacNetObject.GetObjectIdByString(obj.Key);
                var values = new List<BACnetPropertyValue>();
                foreach (var val in obj.Value)
                {
                    var propertyId = val.Key;
                    var valueByType = ConvertValueToBacnet(obj.Key, val.Value, propertyId);
                    var valueToAdd = new BACnetPropertyValue((int)propertyId, valueByType, priority:10);
                    values.Add(valueToAdd);
                }
                dataToWrite.Add(objId, values);
            }
            var writePropertyMultipleRequest = new WritePropertyMultipleRequest(dataToWrite);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, writePropertyMultipleRequest, null, true, settings) == null;
        }

        private List<BACnetDataType> ConvertValueToBacnet(string bacNetObjectId, object value, BacnetPropertyId propertyId)
        {
            if (propertyId == BacnetPropertyId.ObjectName)
                return new List<BACnetDataType> {new BACnetCharacterString(value as string)};
            var objType = new Regex(@"[a-z\-A-Z]+").Match(bacNetObjectId).Value;
            objType = objType.ToUpper();
            if (objType == "AI" || objType == "AO" || objType == "AV")
            {
                var stringValue = value.ToString();
                float res;
                float.TryParse(stringValue, out res);
                return new List<BACnetDataType> {new BACnetReal(res)};
            }
            if (objType == "BI" || objType == "BO" || objType == "BV")
            {
                var stringValue = value.ToString();
                bool res;
                bool.TryParse(stringValue, out res);
                if (stringValue == "1")
                    res = true;
                if (stringValue == "0")
                    res = false;
                if (stringValue.ToLower() == "on")
                    res = true;
                if (stringValue.ToLower() == "off")
                    res = false;
                return new List<BACnetDataType> {new BACnetEnumerated(res ? 1 : 0)};
            }
            if (objType == "MI" || objType == "MO" || objType == "MV")
            {
                var stringValue = value.ToString();
                uint res;
                uint.TryParse(stringValue, out res);
                return new List<BACnetDataType>{new BACnetUnsigned(res)};
            }
            if(objType == "SCH")
            {
                if (propertyId == BacnetPropertyId.WeeklySchedule)
                    return value as List<BACnetDataType>;
                if (propertyId == BacnetPropertyId.ListOfObjectPropertyReferences &&
                    value is List<BACnetDeviceObjectPropertyReference>)
                    return (value as List<BACnetDeviceObjectPropertyReference>).Cast<BACnetDataType>().ToList();
            }
            return null;
        }

        internal object CreateObject(BACnetAddress bacAddress, string address, List<BACnetPropertyValue> data, ApduSettings settings)
        {
            var createObjectRequest = new CreateObjectRequest(BacNetObject.GetObjectIdByString(address), data);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.CreateObject, createObjectRequest, settings:settings);
        }

        internal object DeleteObject(BACnetAddress bacAddress, string address)
        {
            var deleteObjectRequest = new DeleteObjectRequest(BacNetObject.GetObjectIdByString(address));
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.DeleteObject, deleteObjectRequest);
        }        

        internal object SubscribeCOV(BACnetAddress bacAddress, BacNetObject bacNetObject)
        {
            var subscribeCOVRequest = new SubscribeCOVRequest(357, BacNetObject.GetObjectIdByString(bacNetObject.Id), false, 3600);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.SubscribeCOV, subscribeCOVRequest, false);
        }

        #endregion

        #region Request services

        private object SendConfirmedRequest(BACnetAddress bacAddress, BacnetConfirmedServices service, ConfirmedRequest confirmedRequest, object state = null, bool waitForResponse = true, ApduSettings settings = null)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(service, state);
            if (settings != null)
                request.InvokeId = _bacNetProvider.SendMessage(bacAddress, confirmedRequest, settings);
            else
                request.InvokeId = _bacNetProvider.SendMessage(bacAddress, confirmedRequest);
            if (waitForResponse)
            {
                request.ResetEvent.WaitOne(3000);
                RemoveRequest(request);
                return request.State;
            }           
            return null;
        }

        private BacNetRequest CreateRequest(BacnetConfirmedServices serviceChoise, object state = null)
        {
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = new BacNetRequest
                {
                    ServiceChoise = serviceChoise,
                    ResetEvent = new AutoResetEvent(false),
                    State = state
                };
                request.RequestTimeEndedEvent += RemoveRequest;
                _requests.Add(request);
            }
            return request;
        }

        private void RemoveRequest(BacNetRequest request)
        {
            lock (SyncRoot)
            {
                request.RequestTimeEndedEvent -= RemoveRequest;
                _requests.Remove(request);
            }
        }

        #endregion

        #region Acknowledgements

        private void OnIamReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as IAmRequest;
            if (service != null && service.DeviceId.ObjectType == (int) BacnetObjectType.Device)
            {
                if (_deviceList.FindIndex(d => d.Id == (uint) service.DeviceId.Instance) < 0)
                {
                    var device = new BacNetDevice((uint) service.DeviceId.Instance, this);
                    _deviceList.Add(device);
                }
                this[(uint) service.DeviceId.Instance].SetAddress(e.BacnetAddress, service.SegmentationSupport,
                                                                      service.GetApduSettings());
            }
        }

        private void OnReadPropertyAckReceived(object sender, AppServiceEventArgs e)
        {
            
            var service = e.Service as ReadPropertyAck;
            if (service == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == e.InvokeID);
            if (index >= 0)
            {
                if (_requests[index].State == null)
                {
                    _requests[index].State = service;
                    _requests[index].ResetEvent.Set();                    
                }
                if (_requests[index].State is BacNetObject && service.PropertyValues.Count == 1)
                {
                    var bacNetObject = _requests[index].State as BacNetObject;
                    bacNetObject.StringValue = service.PropertyValues[0].ToString();
                    RemoveRequest(_requests[index]);
                }
            }
        }

        private void OnReadPropertyMultipleAckReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as ReadPropertyMultipleAck;
            if (service == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == e.InvokeID);
            if (index >= 0)
            {
                if (_requests[index].State == null)
                {                    
                }
                if (_requests[index].State is List<BacNetObject>)
                {
                    var objectList = _requests[index].State as List<BacNetObject>;
                    foreach (var bacObject in objectList)
                    {
                        var objId = BacNetObject.GetObjectIdByString(bacObject.Id);

                        foreach (var readAccessResult in service.ReadAccessResults)
                        {
                            if (readAccessResult.Key.Instance == objId.Instance && readAccessResult.Key.ObjectType == objId.ObjectType)
                            {
                                var resObject = readAccessResult.Value;
                                if (resObject.Count > 0 && resObject[0].Values.Count > 0)
                                    bacObject.StringValue = resObject.First().Values.First().ToString();
                            }
                        }                            
                    }
                    RemoveRequest(_requests[index]);
                }
            }
        }

        private void OnErrorAckReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as BaseErrorService;
            if (service == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == e.InvokeID);
            if (index >= 0)
            {                
                if (_requests[index].State == null)
                {
                    _requests[index].State = service;
                    _requests[index].ResetEvent.Set();                    
                }                
                if (_requests[index].State is bool && (_requests[index].State as bool?) == false)
                {
                    _requests[index].State = false;
                    _requests[index].ResetEvent.Set();
                }
                if (_requests[index].State is BacNetObject)
                {
                    var bacNetObject = _requests[index].State as BacNetObject;
                    bacNetObject.StringValue = "Error";
                    RemoveRequest(_requests[index]);
                }
            }
        }

        private void OnSubscribeCOVAck(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as SubscribeCOVAck;
            if (service == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == e.InvokeID);
            if (index >= 0)
            {
                _requests[index].State = true;
                _requests[index].ResetEvent.Set();
            }
        }

        private void OnCOVNotification(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as UnconfirmedCOVNotificationRequest;
            if (service == null) return;
            var pvPropertyIndex = service.PropertyValues.FindIndex(s => s.PropertyId.Value == 85);
            if (pvPropertyIndex < 0  || service.PropertyValues[pvPropertyIndex].Values.Count != 1) return;
            var value = service.PropertyValues[pvPropertyIndex].Values[0].ToString();

            int devIndex = _deviceList.FindIndex(d => d.Id == (uint)service.DeviceId.Instance);
            if (devIndex >= 0)
            {
                string objId = BacNetObject.GetStringId((BacnetObjectType) service.ObjectId.ObjectType) +
                               service.ObjectId.Instance;
                if (_deviceList[devIndex].Objects.Contains(objId))
                    _deviceList[devIndex].Objects[objId].StringValue = value;
            }
        }

        private void OnEventNotification(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as UnconfirmedEventNotificationRequest;
            if (service == null) return;

            NotificationEventHandler handler = NotificationEvent;
            if (handler != null) handler(service);
        }

        #endregion
    }    
}