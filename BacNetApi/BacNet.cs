using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using BACsharp;
using BACsharp.AppService;
using BACsharp.AppService.ConfirmedServices;
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

        /*public async Task<object> ReadPropertyAsync(BacNetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId)
        {
            return await Task.Run(() => ReadProperty(bacAddress, address, bacnetPropertyId));
        }*/

        public object ReadProperty(BACnetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId)
        {
            var readPropertyRequest = new ReadPropertyRequest(BacNetObject.GetObjectIdByString(address), (int)bacnetPropertyId);
            var readPropertyResponse = SendConfirmedRequest(bacAddress, BacnetConfirmedServices.ReadProperty, readPropertyRequest) as ReadPropertyAck;
            if (readPropertyResponse == null) return null;
            var values = readPropertyResponse.PropertyValues;
            return values.Count == 1 ? (object) values[0] : values;
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

        private object SendConfirmedRequest(BACnetAddress bacAddress, BacnetConfirmedServices service, ConfirmedRequest confirmedRequest)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(service);
            request.InvokeId = _bacNetProvider.SendMessage(bacAddress, confirmedRequest);
            if (request.ResetEvent.WaitOne(3000))
            {
                RemoveRequest(request);
                return request.Ack;
            }
            return null;
        }

        private void OnReadPropertyAckReceived(object sender, AppServiceEventArgs e)
        {
            
            var service = e.Service as ReadPropertyAck;
            if (service == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == e.InvokeID);
            if (index >= 0)
            {
                _requests[index].Ack = service;
                _requests[index].ResetEvent.Set();
            }
        }

        private void OnErrorAckReceived(object sender, AppServiceEventArgs e)
        {
            /*var apdu = message.Apdu as ErrorAck;
            if (apdu == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == apdu.InvokeId && r.ServiceChoise == BacnetConfirmedServices.ReadProperty);
            if (index >= 0)
            {
                _requests[index].Ack = apdu.ErrorCode;
                _requests[index].ResetEvent.Set();
            }*/
        }

        private BacNetRequest CreateRequest(BacnetConfirmedServices serviceChoise)
        {
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = new BacNetRequest
                {
                    ServiceChoise = serviceChoise,
                    ResetEvent = new AutoResetEvent(false)
                };
                _requests.Add(request);
            }
            return request;
        }

        private void RemoveRequest(BacNetRequest request)
        {
            lock (SyncRoot)
            {
                _requests.Remove(request);
            }
        }
    }    
}