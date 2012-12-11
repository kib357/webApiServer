using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types.Constructed;

namespace BacNetApi.Data
{
    public class PrimitiveObject : BacNetObject
    {
        public PrimitiveObject(BacNetDevice device, string id)
        {
            _device = device;
            Id = id;
            _synchronizationContext = SynchronizationContext.Current;
        }

        private string _stringValue;
        public string StringValue
        {
            get { return _stringValue; }
            set
            {
                _device.LastUpdated = LastUpdated = DateTime.Now;
                if (CheckValueChanges(value))
                {
                    _stringValue = value;
                    if (_synchronizationContext != null)
                        _synchronizationContext.Post(OnValueChangedEvent, _stringValue);
                    else
                        OnValueChangedEvent(_stringValue);
                }
            }
        }

        private bool CheckValueChanges(string value)
        {
            double oldValue, newValue;
            if (double.TryParse(value.Replace(',', '.'), out newValue) &&
                double.TryParse(_stringValue, out oldValue) &&
                Math.Abs(newValue - oldValue) >= 0.1)
                return true;
            return _stringValue != value;
        }

        #region Events
        private readonly List<ValueChangedEventHandler> _valueChangedSubscribers = new List<ValueChangedEventHandler>();
        private event ValueChangedEventHandler _valueChangedEvent;
        public event ValueChangedEventHandler ValueChangedEvent
        {
            add
            {
                _valueChangedEvent += value;
                _valueChangedSubscribers.Add(value);
                _device.AddSubscriptionObject(this);
            }
            remove
            {
                _valueChangedEvent -= value;
                _valueChangedSubscribers.Remove(value);
                if (_valueChangedSubscribers.Count == 0)
                    _device.RemoveSubscriptionObject(this);
            }
        }

        private void OnValueChangedEvent(object state)
        {
            ValueChangedEventHandler handler = _valueChangedEvent;
            if (handler != null) handler(_device.Id + "." + Id, state.ToString());
        }

        #endregion

        #region Methods

        public bool IsExist()
        {
            throw new NotImplementedException();
        }

        public object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue, int arrayIndex = -1)
        {
            return _device.ReadProperty(this, propertyId, arrayIndex);
        }

        public T Get<T>(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            //if (_device.ReadProperty(this, propertyId) is T)
                //return (T)_device.ReadProperty(this, propertyId);
            return default(T);
        }

        public async Task<object> GetAsync(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get(propertyId));
        }

        public async Task<T> GetAsync<T>(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get<T>(propertyId));
        }

        public bool Set(object value, BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return _device.WriteProperty(this, propertyId, value);
        }

        public bool Create(List<BACnetPropertyValue> data = null)
        {
            if (data == null)
                data = new List<BACnetPropertyValue>();
            return _device.CreateObject(this, data);
        }

        public async Task<bool> CreateAsync(List<BACnetPropertyValue> data)
        {
            return await Task.Run(() => Create(data));
        }

        public bool Delete()
        {
            return _device.DeleteObject(this);
        }

        #endregion
    }
}
