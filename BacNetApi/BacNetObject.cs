using System;
using System.Threading.Tasks;
using BacNetTypes;

namespace BacNetApi
{
    public delegate void ValueChangedEventHandler(string address, string value);
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

        public object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {           
            return _device.ReadProperty(this, propertyId);
        }

        public T Get<T>(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            if (_device.ReadProperty(this, propertyId) is T)
                return (T) _device.ReadProperty(this, propertyId);
            return default(T);
        }

        public async Task<object> GetAsync(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get());
        }

        public async Task<T> GetAsync<T>(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get<T>());
        }

        public Task<bool> Set(object value, int propertyId = 85)
        {
            throw new NotImplementedException();
        }

        public bool Create()
        {
            _device.Initialize();
            if (_device.Initialized)
            {
                _device.CreateObject();
                return true;
            }
            return false;
        }

        public async Task<bool> CreateAsync()
        {
            return await Task.Run(() => Create());
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