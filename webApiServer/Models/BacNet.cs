using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace webApiServer.Models
{
    public class BacNet
    {
        private readonly List<BacNetDevice> _deviceList = new List<BacNetDevice>(); 

        public BacNetDevice this[int i]
        {
            get
            {
                int index = _deviceList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new BacNetDevice(i);
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
    }

    public class BacNetDevice
    {
        public int Id { get; private set; }
        public BacNetObjectIndexator Objects { get; set; }

        public BacNetDevice(int id)
        {
            Id = id;
            Objects = new BacNetObjectIndexator(this);
        }

        
    }

    public class BacNetObjectIndexator
    {
        private readonly List<BacNetObject> _objectList = new List<BacNetObject>();
        private readonly BacNetDevice _device;
 
        public BacNetObjectIndexator(BacNetDevice device)
        {
            _device = device;
        }

        public BacNetObject this[string i]
        {
            get
            {
                int index = _objectList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new BacNetObject(_device, i);
                    _objectList.Add(device);
                    index = _objectList.FindIndex(d => d.Id == i);
                }
                return _objectList[index];
            }
            set
            {
                int index = _objectList.FindIndex(d => d.Id == i);
                if (index < 0)
                    _objectList.Add(value);
                else
                    _objectList[index] = value;
            }
        }
    }

    public class BacNetObject : IBacNetObject
    {
        private BacNetDevice _device;
        public string Id { get; private set; }

        public BacNetObject(BacNetDevice device, string id)
        {
            _device = device;
            Id = id;
        }

        public Task<HttpResponseMessage> IsExist()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> Get(int propertyId = 85)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> Set(object value, int propertyId = 85)
        {
            throw new NotImplementedException();
        }

        private event SubscribeEventHandler _subscribeEvent;
        public event SubscribeEventHandler SubscribeEvent
        {
            add { _subscribeEvent += value; }
            remove { _subscribeEvent -= value; }
        }

        private void OnSubscribeEvent()
        {
            SubscribeEventHandler handler = _subscribeEvent;
            if (handler != null) handler();
        }

        public Task<HttpResponseMessage> Create()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> Delete()
        {
            throw new NotImplementedException();
        }
    }

    public delegate void SubscribeEventHandler();

    public interface IBacNetObject
    {
        event SubscribeEventHandler SubscribeEvent;

        Task<HttpResponseMessage> IsExist();
        Task<HttpResponseMessage> Get(int propertyId = 85);
        Task<HttpResponseMessage> Set(object value, int propertyId = 85);        
        Task<HttpResponseMessage> Create();
        Task<HttpResponseMessage> Delete();
    }
}