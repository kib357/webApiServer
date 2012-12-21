using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using BACsharp;
using BACsharp.AppService;
using BACsharp.AppService.Confirmed;
using BACsharp.AppService.Errors;
using BACsharp.AppService.Unconfirmed;
using BACsharp.DataLink;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi.AccessControl;
using BacNetApi.Attributes;
using BacNetApi.Data;

namespace BacNetApi
{
    public enum DeviceStatus
    {
        [StringValue("NotInitialized")]
        NotInitialized,
        [StringValue("Standby")]
        Standby,
        [StringValue("Online")]
        Online,
        [StringValue("Fault")]
        Fault
    }

    public enum SubscriptionStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2
    }

    public delegate void NotificationEventHandler(UnconfirmedEventNotificationRequest notification);
    public delegate void NetworkModelChangedEventHandler();

    public class BacNet : IBacNetServices
    {
        private bool                          _initialized;
        private BaseAppServiceProvider        _bacNetProvider;
        private readonly List<BacNetDevice>   _deviceList = new List<BacNetDevice>();
        private readonly List<BacNetRequest>  _requests = new List<BacNetRequest>();
        public readonly object                SyncRoot = new Object();
        internal readonly DeviceManager       Manager;
        internal readonly BacNetConfig        Config;
        public event NotificationEventHandler NotificationEvent;

        public event NetworkModelChangedEventHandler NetworkModelChangedEvent;

        internal void OnNetworkModelChangedEvent()
        {
            NetworkModelChangedEventHandler handler = NetworkModelChangedEvent;
            if (handler != null) handler();
        }

        public BacNet()
        {
            Config = new BacNetConfig();
            InitializeProvider(Config.IpAddress);
            Manager = new DeviceManager(this);
        }

        public BacNet(string address)
        {
            Config = new BacNetConfig {IpAddress = address};
            InitializeProvider(Config.IpAddress);
            Manager = new DeviceManager(this);
        }

        private void InitializeProvider(string address)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(address, out ipAddress))
                StartProvider(ipAddress);
            else
            {
                throw new Exception("Invalid Ip address configuration");
            }
        }

        private void StartProvider(IPAddress address)
        {
            if (_initialized) return;
            _bacNetProvider = new BaseAppServiceProvider(new DataLinkPort(address));                        
            _bacNetProvider.IAmRequestEvent += OnIamReceived;
            _bacNetProvider.ReadPropertyAckEvent += OnReadPropertyAckReceived;
            _bacNetProvider.ReadPropertyMultipleAckEvent += OnReadPropertyMultipleAckReceived;
            _bacNetProvider.ErrorEvent += OnErrorAckReceived;
            _bacNetProvider.SubscribeCOVAckEvent += OnSubscribeCOVAck;
            _bacNetProvider.UnconfirmedCOVNotificationRequestEvent += OnCOVNotification;
            _bacNetProvider.UnconfirmedEventNotificationRequestEvent += OnEventNotification;
            _bacNetProvider.CreateObjectAckEvent += OnCreateObjectAckReceived;
            _bacNetProvider.DeleteObjectAckEvent += OnDeleteObjectAckReceived;
            _bacNetProvider.Start();

            _initialized = true;
        }

        public BacNetDevice this[uint i]
        {
            get
            {
                BacNetDevice dev;
                lock (_deviceList)
                {
                    if (_deviceList.FindIndex(d => d.Id == i) < 0)
                        _deviceList.Add(new BacNetDevice(i, this));
                    dev = _deviceList.FirstOrDefault(d => d.Id == i);
                }
                return dev;                   
            }
        }

        public void SearchAllDevices()
        {
            Manager.SearchAllDevices();
        }

        public List<BacNetDevice> OnlineDevices
        {
            get
            {
                List<BacNetDevice> list;
                lock (_deviceList)
                {
                    list = new List<BacNetDevice>(_deviceList);
                }
                return list;
            }
        }

        #region Requests

        public void WhoIs(uint startAddress = 0, uint endAddress = 0)
        {
            if (endAddress >= startAddress && endAddress != 0)
                _bacNetProvider.SendMessage(BACnetRemoteAddress.GlobalBroadcast(), new WhoIsRequest(startAddress, endAddress));
            else
                _bacNetProvider.SendMessage(BACnetRemoteAddress.GlobalBroadcast(), new WhoIsRequest());
        }

        internal List<BACnetDataType> ReadProperty(BACnetRemoteAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId, int arrayIndex = -1)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(address), (int)bacnetPropertyId, arrayIndex);
            var readPropertyResponse = SendConfirmedRequest(bacAddress, BacnetConfirmedService.ReadProperty, readPropertyRequest) as ReadPropertyAck;
            if (readPropertyResponse == null) return null;
            return readPropertyResponse.PropertyValues;
        }

        internal void BeginReadProperty(BACnetRemoteAddress bacAddress, BacNetObject bacObject, BacnetPropertyId bacnetPropertyId)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(bacObject.Id), (int)bacnetPropertyId);
            SendConfirmedRequest(bacAddress, BacnetConfirmedService.ReadProperty, readPropertyRequest, bacObject, false);
        }

        internal void BeginReadPropertyMultiple(BACnetRemoteAddress bacAddress, List<BacNetObject> objectList, ApduSettings settings)
        {
            var objList = new Dictionary<BACnetObjectId, List<BACnetPropertyReference>>();
            foreach (var bacObject in objectList)
            {
                objList.Add(BacNetObject.GetObjectIdByString(bacObject.Id), 
                    new List<BACnetPropertyReference>{new BACnetPropertyReference((int)BacnetPropertyId.PresentValue)});
            }
            var readPropertyMultipleRequest = new ReadPropertyMultipleRequest(objList);
            SendConfirmedRequest(bacAddress, BacnetConfirmedService.ReadPropMultiple, readPropertyMultipleRequest, objectList, false, settings);
        }

        internal bool WriteProperty(BACnetRemoteAddress bacAddress, BacNetObject bacNetObject, BacnetPropertyId bacnetPropertyId, object value, ApduSettings settings)
        {
            var objId = BacNetObject.GetObjectIdByString(bacNetObject.Id);
            List<BACnetDataType> valueByType = ConvertValueToBacnet(bacNetObject.Id, value, bacnetPropertyId);
            var writePropertyRequest = new WritePropertyRequest(objId, (int)bacnetPropertyId, valueByType, priority:10);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedService.WriteProperty, writePropertyRequest, null, true, settings) == null;
        }

        internal void BeginWriteProperty(BACnetRemoteAddress bacAddress, BacNetObject bacNetObject, BacnetPropertyId bacnetPropertyId, object value, ApduSettings settings)
        {
            var objId = BacNetObject.GetObjectIdByString(bacNetObject.Id);
            List<BACnetDataType> valueByType = ConvertValueToBacnet(bacNetObject.Id, value, bacnetPropertyId);
            var writePropertyRequest = new WritePropertyRequest(objId, (int)bacnetPropertyId, valueByType, priority: 10);
            SendConfirmedRequest(bacAddress, BacnetConfirmedService.WriteProperty, writePropertyRequest, null, false, settings);
        }

        internal bool WritePropertyMultiple(BACnetRemoteAddress bacAddress, Dictionary<string, Dictionary<BacnetPropertyId, object>> objectIdWithValues, ApduSettings settings)
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
            return SendConfirmedRequest(bacAddress, BacnetConfirmedService.WritePropMultiple, writePropertyMultipleRequest, null, true, settings) == null;
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
            if (objType == "SCH")
            {
                if (propertyId == BacnetPropertyId.WeeklySchedule)
                    return value as List<BACnetDataType>;
                if (propertyId == BacnetPropertyId.ListOfObjectPropertyReferences &&
                    value is List<BACnetDeviceObjectPropertyReference>)
                    return (value as List<BACnetDeviceObjectPropertyReference>).Cast<BACnetDataType>().ToList();
            }
            if (objType == "CU")
            {
                if (propertyId == BacnetPropertyId.CardList &&
                    value is List<Card>)
                {
                    var list = new List<BACnetDataType>();
                    foreach (var card in value as List<Card>)
                    {
                        list.Add(new BACnetEnumerated((int)card.SiteCode, 0));
                        list.Add(new BACnetEnumerated((int)card.Number, 1));
                        list.Add(new BACnetEnumerated((int)card.Status, 2));
                    }
                    return list.ToList();
                }
                if (propertyId == BacnetPropertyId.AccessGroups &&
                    value is List<uint>)
                {
                    var list = new List<BACnetObjectId>();
                    foreach (var ag in value as List<uint>)
                        list.Add(new BACnetObjectId((int)BacnetObjectType.AccessGroup, (int)ag));
                    return list.Cast<BACnetDataType>().ToList();
                }
            }
            if (objType == "AG")
            {
                if ((propertyId == BacnetPropertyId.AccessAreas || propertyId == BacnetPropertyId.AccessAreasExceptions) &&
                    value is List<AccessArea>)
                {
                    var list = new List<BACnetDataType>();
                    foreach (var area in value as List<AccessArea>)
                    {
                        var objectId = new BACnetObjectId((int)area.Type, (int)area.InstanceNumber, 0);
                        var areaSequence = new Sequence(new List<BACnetDataType> { objectId, new BACnetEnumerated(85, 1) }, 0);
                        Sequence scheduleSequence;
                        if (area.ScheduleId != 0)
                        {
                            var scheduleId = new BACnetObjectId((int)BacnetObjectType.Schedule, (int)area.ScheduleId, 0);
                            scheduleSequence = new Sequence(new List<BACnetDataType> { scheduleId, new BACnetEnumerated(85, 1) }, 1);
                        }
                        else
                            scheduleSequence = new Sequence(new List<BACnetDataType> { new BACnetUnsigned(0xFFFFFFFF, 0), new BACnetUnsigned(0x3FFFFE, 1), new BACnetUnsigned(0x023FFFFF, 2) }, 1);
                        list.Add(areaSequence);
                        list.Add(scheduleSequence);
                    }
                    return list.ToList();
                }
            }
            return null;
        }

        internal object CreateObject(BACnetRemoteAddress bacAddress, string address, List<BACnetPropertyValue> data, ApduSettings settings)
        {
            var createObjectRequest = new CreateObjectRequest(BacNetObject.GetObjectIdByString(address), data);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedService.CreateObject, createObjectRequest, settings:settings);
        }

        internal object DeleteObject(BACnetRemoteAddress bacAddress, string address)
        {
            var deleteObjectRequest = new DeleteObjectRequest(BacNetObject.GetObjectIdByString(address));
            return SendConfirmedRequest(bacAddress, BacnetConfirmedService.DeleteObject, deleteObjectRequest);
        }

        internal object SubscribeCOV(BACnetRemoteAddress bacAddress, BacNetObject bacNetObject)
        {
            var subscribeCOVRequest = new SubscribeCOVRequest(357, BacNetObject.GetObjectIdByString(bacNetObject.Id), false, 3600);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedService.SubscribeCOV, subscribeCOVRequest, false);
        }

        #endregion

        #region Request services

        private object SendConfirmedRequest(BACnetRemoteAddress bacAddress, BacnetConfirmedService service, ConfirmedRequest confirmedRequest, object state = null, bool waitForResponse = true, ApduSettings settings = null)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(service, state);
            if (settings != null)
                request.InvokeId = _bacNetProvider.SendMessage(bacAddress, confirmedRequest, settings);
            else
                request.InvokeId = _bacNetProvider.SendMessage(bacAddress, confirmedRequest);
            if (waitForResponse)
            {
                request.ResetEvent.WaitOne(TimeSpan.FromSeconds(Config.RequestTimeOut));
                RemoveRequest(request);
                return request.State;
            }           
            return null;
        }

        private BacNetRequest CreateRequest(BacnetConfirmedService serviceChoise, object state = null)
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
                Manager.DeviceLocated((uint) service.DeviceId.Instance, e.RemoteAddress,
                                      service.SegmentationSupport, service.GetApduSettings());
            }
        }

        private void OnReadPropertyAckReceived(object sender, AppServiceEventArgs e)
        {
            
            var service = e.Service as ReadPropertyAck;
            if (service == null) return;
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = _requests.FirstOrDefault(r => r.InvokeId == e.InvokeID);   
            }            
            if (request != null)
            {
                if (request.State == null)
                {
                    request.State = service;
                    request.ResetEvent.Set();                    
                }
                if (request.State is PrimitiveObject && service.PropertyValues.Count == 1)
                {
                    var bacNetObject = request.State as PrimitiveObject;
                    bacNetObject.StringValue = service.PropertyValues[0].ToString();
                    RemoveRequest(request);
                }
            }
        }

        private void OnReadPropertyMultipleAckReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as ReadPropertyMultipleAck;
            if (service == null) return;
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = _requests.FirstOrDefault(r => r.InvokeId == e.InvokeID);
            } 
            if (request != null)
            {
                if (request.State == null)
                {                    
                }
                if (request.State is List<PrimitiveObject>)
                {
                    var objectList = request.State as List<PrimitiveObject>;
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
                    RemoveRequest(request);
                }
            }
        }

        private void OnErrorAckReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as BaseErrorService;
            if (service == null) return;
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = _requests.FirstOrDefault(r => r.InvokeId == e.InvokeID);
            }
            if (request != null)
            {
                if (request.State == null)
                {
                    request.State = service;
                    request.ResetEvent.Set();                    
                }
                if (request.State is bool && (request.State as bool?) == false)
                {
                    request.State = false;
                    request.ResetEvent.Set();
                }
                if (request.State is PrimitiveObject)
                {
                    var bacNetObject = request.State as PrimitiveObject;
                    bacNetObject.StringValue = "Error";
                    RemoveRequest(request);
                }
            }
        }

        private void OnSubscribeCOVAck(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as SubscribeCOVAck;
            if (service == null) return;
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = _requests.FirstOrDefault(r => r.InvokeId == e.InvokeID);
            }
            if (request != null)
            {
                request.State = true;
                request.ResetEvent.Set();
            }
        }

        private void OnCOVNotification(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as UnconfirmedCOVNotificationRequest;
            if (service == null) return;
            var pvPropertyIndex = service.PropertyValues.FindIndex(s => s.PropertyId.Value == 85);
            if (pvPropertyIndex < 0  || service.PropertyValues[pvPropertyIndex].Values.Count != 1) return;
            var value = service.PropertyValues[pvPropertyIndex].Values[0].ToString();

            BacNetDevice dev;
            lock (_deviceList)
            {
                dev = _deviceList.FirstOrDefault(d => d.Id == (uint)service.DeviceId.Instance);   
            }            
            if (dev != null)
            {
                string objId = BacNetObject.GetStringId((BacnetObjectType) service.ObjectId.ObjectType) +
                               service.ObjectId.Instance;
                if (dev.Objects.Contains(objId))
                    dev.Objects[objId].StringValue = value;
            }
        }

        private void OnCreateObjectAckReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as CreateObjectAck;
            if (service == null) return;
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = _requests.FirstOrDefault(r => r.InvokeId == e.InvokeID);
            }
            if (request != null)
            {
                request.State = true;
                request.ResetEvent.Set();
            }
        }

        private void OnDeleteObjectAckReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as DeleteObjectAck;
            if (service == null) return;
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = _requests.FirstOrDefault(r => r.InvokeId == e.InvokeID);
            }
            if (request != null)
            {
                request.State = true;
                request.ResetEvent.Set();
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