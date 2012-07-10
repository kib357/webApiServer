using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types.Primitive;

namespace BacNetApi
{
    public class BacNetDevice 
    {        
        private BacNet _network;
        private bool _initialized;
        private bool _isInitializing;
        private readonly AutoResetEvent _waitForAddress = new AutoResetEvent(false);
        private DeviceStatus Status;

        public BACnetAddress                 Address { get; set; }
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
            Status = DeviceStatus.NotInitialized;
        }     
   
        public async void InitializeAsync()
        {
            /*if (Address == null) await SearchDeviceAsync();
            ServicesSupported = await _network.ReadPropertyAsync(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported) 
                                      as List<BacnetServicesSupported>;
            _initialized = true; */
        }

        public void Initialize()
        {
            if (Status == DeviceStatus.Ready || Status == DeviceStatus.Initializing) return;
            Status = DeviceStatus.Initializing;
            if (Address != null || SearchDevice())
            {                
                var services = _network.ReadProperty(Address, Id + ".DEV" + Id, BacnetPropertyId.ProtocolServicesSupported);
                if (services is BACnetBitString)
                {
                    var value = Convert.ToString((services as BACnetBitString).Value, 2);
                    for (int i = 0; i <value.Length && i < (int)BacnetServicesSupported.MaxBacnetServicesSupported; i++)
                    {
                        if (value[i] == '1')
                            ServicesSupported.Add((BacnetServicesSupported)i);
                    }
                    Status = DeviceStatus.Ready;
                    return;
                }
            }
            Status = DeviceStatus.NotInitialized;
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
                                   while (Status != DeviceStatus.Ready)
                                   {
                                       Initialize();
                                       if (Status == DeviceStatus.Ready) break;
                                       Thread.Sleep(TimeSpan.FromMinutes(minutes));
                                       if (minutes < 127) minutes *= 2;
                                   }
                                   return null;
                               });
        }

        public void CreateObject()
        {
            throw new NotImplementedException();
        }

        public object ReadProperty(BacNetObject bacNetObject, BacnetPropertyId propertyId)
        {
            Initialize();
            if (!Initialized) return null;
            return _network.ReadProperty(Address, bacNetObject.Id, propertyId);           
        }

        public void SetAddress(BACnetAddress source, BACnetEnumerated segmentationSupported)
        {
            Address = source;
            Segmentation = (BacnetSegmentation)segmentationSupported.Value;
            _waitForAddress.Set();
        }
    }
}