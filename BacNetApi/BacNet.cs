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

namespace BacNetApi
{
    enum DeviceStatus
    {
        NotInitialized = 0,
        Initializing = 1,
        Ready = 2,
    }

    enum SubscriptionStatus
    {
        Stopped = 0,
        Initializing = 1,
        Running = 2
    }

    public delegate void NotificationEventHandler(UnconfirmedEventNotificationRequest notification);

    public class BacNet : IBacNetServices
    {
        private bool                         _initialized;
        private BaseAppServiceProvider       _bacNetProvider;
        private readonly List<BacNetDevice>  _deviceList = new List<BacNetDevice>();
        private readonly List<BacNetRequest> _requests = new List<BacNetRequest>();
        private static readonly object       SyncRoot = new Object();
        public event NotificationEventHandler NotificationEvent;

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
            _bacNetProvider.OnError += OnErrorAckReceived;
            _bacNetProvider.OnSubscribeCOVAck += OnSubscribeCOVAck;
            _bacNetProvider.OnUnconfirmedCOVNotificationRequest += OnCOVNotification;
            _bacNetProvider.OnUnconfirmedEventNotificationRequest += OnEventNotification;
            _bacNetProvider.Start();

            _initialized = true;
        }       

        private void OnIamReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as IAmRequest;
            if (service != null && service.DeviceId.ObjectType == (int)BacnetObjectType.Device &&
                _deviceList.FindIndex(d => d.Id == (uint)service.DeviceId.Instance) >= 0)
            {
                this[(uint)service.DeviceId.Instance].SetAddress(e.BacnetAddress, service.SegmentationSupport, service.GetApduSettings());
            }
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
            }
        }

        internal void WhoIs(ushort startAddress, ushort endAddress)
        {
            _bacNetProvider.SendMessage(BACnetAddress.GlobalBroadcast(), new WhoIsRequest(startAddress, endAddress));
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

        internal bool WriteProperty(BACnetAddress bacAddress, BacNetObject bacNetObject, BacnetPropertyId bacnetPropertyId, object value, ApduSettings settings)
        {
            var objId = BacNetObject.GetObjectIdByString(bacNetObject.Id);
            List<BACnetDataType> valueByType = ConvertValueToBacnet(bacNetObject.Id, value, bacnetPropertyId);
            var writePropertyRequest = new WritePropertyRequest2(objId, (int)bacnetPropertyId, valueByType);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, writePropertyRequest, null, true, settings) == null;
        }

        private List<BACnetDataType> ConvertValueToBacnet(string bacNetObjectId, object value, BacnetPropertyId propertyId)
        {
            if (propertyId == BacnetPropertyId.ObjectName)
                return new List<BACnetDataType> { value as BACnetCharacterString };
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
                if (propertyId == BacnetPropertyId.ListOfObjectPropertyReferences)
                    return (value as List<BACnetDeviceObjectPropertyReference>).Cast<BACnetDataType>().ToList();
            }
            return null;
        }

        internal object CreateObject(BACnetAddress bacAddress, string address)
        {
            var createObjectRequest = new CreateObjectRequest(BacNetObject.GetObjectIdByString(address), new List<BACnetPropertyValue>());
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.CreateObject, createObjectRequest);
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
    }    
}