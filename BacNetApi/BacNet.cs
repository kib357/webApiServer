using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BACSharp;
using BACSharp.NPDU;
using BACSharp.Network;
using BACSharp.Services.Acknowledgement;
using BACSharp.Services.Unconfirmed;
using BACSharp.Types;
using BacNetTypes;

namespace BacNetApi
{
    public class BacNet : IBacNetServices
    {
        private bool                         _initialized;
        private BacNetProvider               _bacNetProvider;
        private readonly List<BacNetDevice>  _deviceList = new List<BacNetDevice>();
        private readonly List<BacNetRequest> _requests = new List<BacNetRequest>();
        private static readonly object       SyncRoot = new Object();

        private byte _invokeId;
        private byte InvokeId { get { return _invokeId++; } }

        public void Initialize(IPAddress address)
        {
            if (_initialized) return;

            _bacNetProvider = BacNetProvider.Instance;
            _bacNetProvider.DeviceId = 357;
            _bacNetProvider.Network = new BacNetIpNetwork(address, IPAddress.Parse("255.255.255.0"));
            _bacNetProvider.Response.ReceivedIAmEvent += OnIamReceived;
            _bacNetProvider.Response.ReceivedReadPropertyAckEvent += OnReadPropertyAckReceived;
            _bacNetProvider.Response.ReceivedErrorAckEvent += OnErrorAckReceived;

            _initialized = true;
        }

        private void OnIamReceived(BacNetMessage message)
        {
            var npdu = message.Npdu as BacNetIpNpdu;
            var apdu = message.Apdu as IAm;
            if (npdu == null || apdu == null) return;
            if (_deviceList.FindIndex(d => d.Id == apdu.deviceObject.ObjectId) >= 0)
            {
                this[apdu.deviceObject.ObjectId].SetAddress(npdu.Source, apdu.SegmentationSupported);
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
            _bacNetProvider.Services.Unconfirmed.WhoIs(startAddress, endAddress);
        }

        public async Task<object> ReadPropertyAsync(BacNetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId)
        {
            return await Task.Run(() => ReadProperty(bacAddress, address, bacnetPropertyId));
        }

        public object ReadProperty(BacNetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(BacnetConfirmedServices.ReadProperty);
            _bacNetProvider.Services.Confirmed.ReadProperty(bacAddress, address, bacnetPropertyId);
            if (request.ResetEvent.WaitOne(3000))
            {
                RemoveRequest(request);
                return request.Ack;
            }
            return null;
        }

        public T ReadProperty<T>(BacNetAddress bacAddress, string address, BacnetPropertyId bacnetPropertyId)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(BacnetConfirmedServices.ReadProperty);
            _bacNetProvider.Services.Confirmed.ReadProperty(bacAddress, address, bacnetPropertyId);
            if (request.ResetEvent.WaitOne(3000))
            {
                RemoveRequest(request);
                var ack = request.Ack as ReadPropertyAck;
                if (ack != null && ack.Value is T)
                return (T)ack.Value;
            }
            return default(T);
        }

        public async Task<object> CreatePropertyAsync(BacNetAddress bacAddress, string address)
        {
            return await Task.Run(() => CreateProperty(bacAddress, address));
        }

        public object CreateProperty(BacNetAddress bacAddress, string address)
        {
            if (!_initialized) throw new Exception("Network provider not initialized");
            var request = CreateRequest(BacnetConfirmedServices.ReadProperty);
            //_bacNetProvider.Services.Confirmed.CreateObject(bacAddress, address);
            if (request.ResetEvent.WaitOne(3000))
            {
                RemoveRequest(request);
                return request.Ack;
            }
            return null;
        }

        private void OnReadPropertyAckReceived(BacNetMessage message)
        {
            var apdu = message.Apdu as ReadPropertyAck;
            if (apdu == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == apdu.InvokeId && r.ServiceChoise == BacnetConfirmedServices.ReadProperty);
            if (index >= 0)
            {
                _requests[index].Ack = apdu;
                _requests[index].ResetEvent.Set();
            }
        }

        private void OnErrorAckReceived(BacNetMessage message)
        {
            var apdu = message.Apdu as ErrorAck;
            if (apdu == null) return;
            int index = _requests.FindIndex(r => r.InvokeId == apdu.InvokeId && r.ServiceChoise == BacnetConfirmedServices.ReadProperty);
            if (index >= 0)
            {
                _requests[index].Ack = apdu.ErrorCode;
                _requests[index].ResetEvent.Set();
            }
        }

        private BacNetRequest CreateRequest(BacnetConfirmedServices serviceChoise)
        {
            BacNetRequest request;
            lock (SyncRoot)
            {
                request = new BacNetRequest
                {
                    InvokeId = InvokeId,
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