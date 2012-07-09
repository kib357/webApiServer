using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BacNetTypes;
using BacNetTypes.Primitive;

namespace BacNetApi
{
    public class BacNetDevice 
    {        
        private BacNet _network;
        private bool _initialized;
        private readonly AutoResetEvent _waitForAddress = new AutoResetEvent(false);

        public BacNetAddress                 Address { get; set; }
        public uint                          Id { get; private set; }
        public bool                          Initialized { get { return _initialized; } }
        public BacNetObjectIndexer           Objects { get; private set; }
        public BacnetSegmentation            Segmentation { get; set; }
        public List<BacnetServicesSupported> ServicesSupported { get; set; } 

        public BacNetDevice(uint id, BacNet network)
        {
            Id = id;
            _network = network;
            Objects = new BacNetObjectIndexer(this);
            ServicesSupported = new List<BacnetServicesSupported>();
        }     
   
        public async void InitializeAsync()
        {
            if (Address == null) await SearchDeviceAsync();
            ServicesSupported = await _network.ReadPropertyAsync(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported) 
                                      as List<BacnetServicesSupported>;
            _initialized = true; 
        }

        public void Initialize()
        {
            if (Initialized) return;

            if (Address != null || SearchDevice())
            {
                var services = _network.ReadProperty<BacNetBitString>(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported);
                if (services != null)
                {
                    for (int i = 0; i < services.Value.Length; i++)
                    {
                        if (services.Value[i] == '1')
                            ServicesSupported.Add((BacnetServicesSupported)i);
                    }
                    _initialized = true;
                }
            }
        }

        private bool SearchDevice()
        {
            _network.WhoIs((ushort)Id,(ushort)Id);
            _waitForAddress.WaitOne(1000);
            return Address != null;
        }

        private Task SearchDeviceAsync()
        {
            var minutes = 1;
            while (Address == null)
            {
                _network.WhoIs((ushort)Id,(ushort)Id);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                if (Address == null)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(minutes));
                    if (minutes < 127) minutes *= 2;
                }
            }
            return null;
        }

        public void CreateObject()
        {
            throw new NotImplementedException();
        }

        public T ReadProperty<T>(BacNetObject bacNetObject, BacnetPropertyId propertyId)
        {
            Initialize();
            if (!Initialized) return default(T);
            return _network.ReadProperty<T>(Address, Id + "." + bacNetObject.Id, propertyId);
        }

        public object ReadProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId)
        {
            return ReadProperty<object>(bacNetObject, propertyId);
        }

        public void SetAddress(BacNetAddress source, BacnetSegmentation segmentationSupported)
        {
            Address = source;
            Segmentation = segmentationSupported;
            _waitForAddress.Set();
        }
    }
}