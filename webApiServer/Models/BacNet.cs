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
        private readonly List<BacNetObject> _objectList = new List<BacNetObject>(); 

        public BacNetDevice(int id)
        {
            Id = id;
        }

        public BacNetObject this[string i]
        {
            get
            {
                int index = _objectList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new BacNetObject(i);
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
        public string Id { get; private set; }

        public BacNetObject(string id)
        {
            Id = id;
        }

        public bool IsExist()
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

        public event SubscribeEventHandler SubscribeEvent;

        public void OnSubscribeEvent()
        {
            SubscribeEventHandler handler = SubscribeEvent;
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