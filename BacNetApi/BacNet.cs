using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using BACsharp;
using BACsharp.AppService;
using BACsharp.AppService.ConfirmedServices;
using BACsharp.AppService.ErrorServices;
using BACsharp.AppService.UnconfirmedServices;
using BACsharp.DataLink;
using BACsharp.Types;
using BACsharp.Types.Constructed;

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

    public class BacNet : IBacNetServices
    {
        private bool                         _initialized;
        private BaseAppServiceProvider       _bacNetProvider;
        private readonly List<BacNetDevice>  _deviceList = new List<BacNetDevice>();
        private readonly List<BacNetRequest> _requests = new List<BacNetRequest>();
        private static readonly object       SyncRoot = new Object();

        private byte _invokeId;
        private byte InvokeId { get { return _invokeId++; } }

        public void Initialize(string address)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(address, out ipAddress))
                Initialize(address);
        }

        public void Initialize(IPAddress address)
        {
            if (_initialized) return;

            _bacNetProvider = new BaseAppServiceProvider(new DataLinkPort(address));            
            _bacNetProvider.OnIAmRequest += OnIamReceived;
            _bacNetProvider.OnReadPropertyAck += OnReadPropertyAckReceived;
            _bacNetProvider.OnError += OnErrorAckReceived;
            _bacNetProvider.OnSubscribeCOVAck += OnSubscribeCOVAck;
            _bacNetProvider.OnUnconfirmedCOVNotificationRequest += OnCOVNotification;
            _bacNetProvider.Start();

            _initialized = true;
        }        

        private void OnIamReceived(object sender, AppServiceEventArgs e)
        {
            var service = e.Service as IAmRequest;
            if (service != null && service.DeviceId.ObjectType == (int)BacnetObjectType.Device &&
                _deviceList.FindIndex(d => d.Id == (uint)service.DeviceId.Instance) >= 0)
            {
                this[(uint)service.DeviceId.Instance].SetAddress(e.BacnetAddress, service.SegmentationSupport);
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

        public void WhoIs(ushort startAddress, ushort endAddress)
        {
            _bacNetProvider.SendMessage(BACnetAddress.GlobalBroadcast(), new WhoIsRequest(startAddress, endAddress));
        }

        public object ReadProperty(BACnetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(address), (int)bacnetPropertyId);
            var readPropertyResponse = SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, readPropertyRequest) as ReadPropertyAck;
            if (readPropertyResponse == null) return null;
            var values = readPropertyResponse.PropertyValues;
            return values.Count == 1 ? (object) values[0] : values;
        }

        public void BeginReadProperty(BACnetAddress bacAddress, BacNetObject bacObject, BacnetPropertyId bacnetPropertyId)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(bacObject.Id), (int)bacnetPropertyId);
            SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, readPropertyRequest, bacObject, false);
        }

        public object WriteProperty(BACnetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId, BACnetDataType value)
        {
            var writePropertyRequest = new WritePropertyRequest(BacNetObject.GetObjectIdByString(address), (int)bacnetPropertyId, value);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, writePropertyRequest);
        }

        public object CreateObject(BACnetAddress bacAddress, string address)
        {
            var createObjectRequest = new CreateObjectRequest(BacNetObject.GetObjectIdByString(address), new List<BACnetPropertyValue>());
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.CreateObject, createObjectRequest);
        }

        public object SubscribeCOV(BACnetAddress bacAddress, BacNetObject bacNetObject)
        {
            var subscribeCOVRequest = new SubscribeCOVRequest(357, BacNetObject.GetObjectIdByString(bacNetObject.Id), false, 3600);
            return SendConfirmedRequest(bacAddress, BacnetConfirmedServices.SubscribeCOV, subscribeCOVRequest, false);
        }

        private object SendConfirmedRequest(BACnetAddress bacAddress, BacnetConfirmedServices service, ConfirmedRequest confirmedRequest, object state = null, bool waitForResponse = true)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(service, state);
            request.InvokeId = _bacNetProvider.SendMessage(bacAddress, confirmedRequest);
            if (waitForResponse && request.ResetEvent.WaitOne(3000))
            {
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
    }    
}