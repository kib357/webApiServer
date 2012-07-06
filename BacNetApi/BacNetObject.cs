using System;
using System.Threading.Tasks;

namespace BacNetApi
{
    public class BacNetObject : IBacNetObject
    {
        private BacNetDevice _device;
        public string Id { get; private set; }

        public BacNetObject(BacNetDevice device, string id)
        {
            _device = device;
            Id = id;
        }

        #region Methods

        public Task<bool> IsExist()
        {
            throw new NotImplementedException();
        }

        public Task<string> Get(int propertyId = 85)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Set(object value, int propertyId = 85)
        {
            throw new NotImplementedException();
        }        

        public Task<bool> Create()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Delete()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Events

        private event ValueChangedEventHandler _valueChangedEvent;
        public event ValueChangedEventHandler ValueChangedEvent
        {
            add { _valueChangedEvent += value; }
            remove { _valueChangedEvent -= value; }
        }

        private void OnValueChangedEvent(string address, string value)
        {
            ValueChangedEventHandler handler = _valueChangedEvent;
            if (handler != null) handler(address, value);
        }

        #endregion
    }
}